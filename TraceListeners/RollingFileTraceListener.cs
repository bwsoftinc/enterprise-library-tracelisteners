using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Practices.EnterpriseLibrary.Common.Utility;
using Microsoft.Practices.EnterpriseLibrary.Logging.Formatters;
using Microsoft.Practices.EnterpriseLibrary.Logging.TraceListeners;

namespace BWSoftInc.EnterpriseLogging.TraceListeners
{
    public class RollingFileTraceListener : FlatFileTraceListener
    {
        public const string DefaultSeparator = "----------------------------------------";
        private readonly RollFileExistsBehavior rollFileExistsBehavior;
        private readonly StreamWriterHelper rollingHelper;
        private readonly RollInterval rollInterval;
        private readonly string timeStampPattern;
        private readonly int maxArchivedFiles;
        private readonly int rollSizeInBytes;
        private readonly Timer timer;
        private bool disposed;

        public RollingFileTraceListener(string fileName,
            string header = DefaultSeparator,
            string footer = DefaultSeparator,
            ILogFormatter formatter = null,
            int rollSizeKB = 0,
            string timeStampPattern = "yyyy-MM-dd",
            RollFileExistsBehavior rollFileExistsBehavior = RollFileExistsBehavior.Overwrite,
            RollInterval rollInterval = RollInterval.None,
            int maxArchivedFiles = 0)
            : base(fileName, header, footer, formatter)
        {
            Guard.ArgumentNotNullOrEmpty(fileName, "fileName");
            this.rollSizeInBytes = rollSizeKB * 1024;
            this.timeStampPattern = timeStampPattern;
            this.rollFileExistsBehavior = rollFileExistsBehavior;
            this.rollInterval = rollInterval;
            this.maxArchivedFiles = maxArchivedFiles;
            rollingHelper = new StreamWriterHelper(this);
            if (rollInterval == RollInterval.Midnight)
            {
                var now = DateTime.Now;
                var midnight = now.AddDays(1).Date;
                this.timer = new Timer((o) => this.rollingHelper.RollIfNecessary(), null, midnight.Subtract(now), TimeSpan.FromDays(1));
            }
        }

        public override void TraceData(
            TraceEventCache eventCache,
            string source,
            TraceEventType eventType,
            int id,
            object data)
        {
            rollingHelper.RollIfNecessary();
            base.TraceData(eventCache, source, eventType, id, data);
        }

        protected override void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    base.Dispose(disposing);
                    if (this.timer != null)
                        this.timer.Dispose();
                }
                this.disposed = true;
            }
        }

        ~RollingFileTraceListener()
        {
            this.Dispose(false);
        }
        
        public sealed class StreamWriterHelper
        {
            private RollingFileTraceListener owner;
            private bool performsRolling;
            private TallyKeepingFileStreamWriter managedWriter;
            public DateTime? nextRollDateTime;

            public StreamWriterHelper(RollingFileTraceListener owner)
            {
                this.owner = owner;
                this.performsRolling = this.owner.rollInterval != RollInterval.None || this.owner.rollSizeInBytes > 0;
            }

            public DateTime CalculateNextRollDate(DateTime dateTime)
            {
                switch (this.owner.rollInterval)
                {
                    case RollInterval.Minute:
                        return dateTime.AddMinutes(1);
                    case RollInterval.Hour:
                        return dateTime.AddHours(1);
                    case RollInterval.Day:
                        return dateTime.AddDays(1);
                    case RollInterval.Week:
                        return dateTime.AddDays(7);
                    case RollInterval.Month:
                        return dateTime.AddMonths(1);
                    case RollInterval.Year:
                        return dateTime.AddYears(1);
                    case RollInterval.Midnight:
                        return dateTime.AddDays(1).Date;
                    default:
                        return DateTime.MaxValue;
                }
            }

            public DateTime? CheckIsRollNecessary()
            {
                // check for size roll, if enabled.
                if (this.owner.rollSizeInBytes > 0
                    && (this.managedWriter != null && this.managedWriter.Tally > this.owner.rollSizeInBytes))
                {
                    return DateTime.Now;
                }
                // check for date roll, if enabled.
                DateTime currentDateTime = DateTime.Now;
                if (this.owner.rollInterval != RollInterval.None
                    && (this.nextRollDateTime != null && currentDateTime.CompareTo(this.nextRollDateTime.Value) >= 0))
                {
                    return currentDateTime;
                }

                return null;
            }

            public string ComputeArchiveFileName(string actualFileName, DateTime currentDateTime)
            {
                string directory = Path.GetDirectoryName(actualFileName);
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(actualFileName);
                string extension = Path.GetExtension(actualFileName);
                StringBuilder fileNameBuilder = new StringBuilder(fileNameWithoutExtension);
                if (!string.IsNullOrEmpty(this.owner.timeStampPattern))
                {
                    fileNameBuilder.Append('.');
                    fileNameBuilder.Append(File.GetCreationTime(actualFileName).ToString(this.owner.timeStampPattern, CultureInfo.InvariantCulture));
                }
                if (this.owner.rollFileExistsBehavior == RollFileExistsBehavior.Increment)
                {
                    // look for max sequence for date
                    int newSequence = FindMaxSequenceNumber(directory, fileNameBuilder.ToString(), extension) + 1;
                    fileNameBuilder.Append('.');
                    fileNameBuilder.Append(newSequence.ToString(CultureInfo.InvariantCulture));
                }
                fileNameBuilder.Append(extension);
                return Path.Combine(directory, fileNameBuilder.ToString());
            }

            public static int FindMaxSequenceNumber(string directoryName, string fileName, string extension)
            {
                string[] existingFiles =
                Directory.GetFiles(directoryName, string.Format(CultureInfo.InvariantCulture, "{0}*{1}", fileName, extension));
                int maxSequence = 0;
                Regex regex = new Regex(string.Format(CultureInfo.InvariantCulture, @"{0}\.(?<sequence>\d+){1}$", fileName, extension));
                for (int i = 0; i < existingFiles.Length; i++)
                {
                    Match sequenceMatch = regex.Match(existingFiles[i]);
                    if (sequenceMatch.Success)
                    {
                        int currentSequence = 0;
                        string sequenceInFile = sequenceMatch.Groups["sequence"].Value;
                        if (!int.TryParse(sequenceInFile, out currentSequence))
                        {
                            continue;
                            // very unlikely
                        }
                        if (currentSequence > maxSequence)
                        {
                            maxSequence = currentSequence;
                        }
                    }
                }
                return maxSequence;
            }

            public void PerformRoll(DateTime rollDateTime)
            {
                string actualFileName = ((FileStream)((StreamWriter)this.owner.Writer).BaseStream).Name;
                if (this.owner.rollFileExistsBehavior == RollFileExistsBehavior.Overwrite
                    && string.IsNullOrEmpty(this.owner.timeStampPattern))
                {
                    // no roll will be actually performed: no timestamp pattern is available, and
                    // the roll behavior is overwrite, so the original file will be truncated
                    this.owner.Writer.Close();
                    File.WriteAllText(actualFileName, string.Empty);
                }
                else
                {
                    // calculate archive name
                    string archiveFileName = this.ComputeArchiveFileName(actualFileName, rollDateTime);
                    // close file
                    this.owner.Writer.Close();
                    // move file
                    this.SafeMove(actualFileName, archiveFileName, rollDateTime);
                    // purge if necessary
                    this.PurgeArchivedFiles(actualFileName);
                }
                // update writer - let TWTL open the file as needed to keep consistency
                this.owner.Writer = null;
                this.managedWriter = null;
                this.nextRollDateTime = null;
                this.UpdateRollingInformationIfNecessary();
            }

            public void RollIfNecessary()
            {
                if (!this.performsRolling)
                {
                    // avoid further processing if no rolling has been configured.
                    return;
                }
                if (!this.UpdateRollingInformationIfNecessary())
                {
                    // an error was detected while handling roll information - avoid further processing
                    return;
                }
                DateTime? rollDateTime;
                if ((rollDateTime = this.CheckIsRollNecessary()) != null)
                {
                    this.PerformRoll(rollDateTime.Value);
                }
            }

            public bool UpdateRollingInformationIfNecessary()
            {
                StreamWriter currentWriter = null;
                // replace writer with the tally keeping version if necessary for size rolling
                if (this.owner.rollSizeInBytes > 0 && this.managedWriter == null)
                {
                    currentWriter = this.owner.Writer as StreamWriter;
                    if (currentWriter == null)
                    {
                        // TWTL couldn't acquire the writer - abort
                        return false;
                    }
                    var actualFileName = ((FileStream)currentWriter.BaseStream).Name;
                    currentWriter.Close();
                    FileStream fileStream = null;
                    try
                    {
                        fileStream = File.Open(actualFileName, FileMode.Append, FileAccess.Write, FileShare.Read);
                        this.managedWriter = new TallyKeepingFileStreamWriter(fileStream, GetEncodingWithFallback());
                    }
                    catch (IOException)
                    {
                        // there's a slight chance of error here - abort if this occurs and just let TWTL handle it without attempting to roll
                        return false;
                    }
                    this.owner.Writer = this.managedWriter;
                }
                // compute the next roll date if necessary
                if (this.owner.rollInterval != RollInterval.None && this.nextRollDateTime == null)
                {
                    try
                    {
                        // casting should be safe at this point - only file stream writers can be the writers for the owner trace listener.
                        // it should also happen rarely
                        this.nextRollDateTime
                            = this.CalculateNextRollDate(File.GetCreationTime(((FileStream)((StreamWriter)this.owner.Writer).BaseStream).Name));
                    }
                    catch (IOException)
                    {
                        this.nextRollDateTime = DateTime.MaxValue;
                        // disable rolling if no date could be retrieved.
                        // there's a slight chance of error here - abort if this occurs and just let TWTL handle it without attempting to roll
                        return false;
                    }
                }
                return true;
            }

            private static Encoding GetEncodingWithFallback()
            {
                Encoding encoding = (Encoding)new UTF8Encoding(false).Clone();
                encoding.EncoderFallback = EncoderFallback.ReplacementFallback;
                encoding.DecoderFallback = DecoderFallback.ReplacementFallback;
                return encoding;
            }

            private void SafeMove(string actualFileName, string archiveFileName, DateTime currentDateTime)
            {
                try
                {
                    if (File.Exists(archiveFileName))
                    {
                        File.Delete(archiveFileName);
                    }
                    // take care of tunneling issues http://support.microsoft.com/kb/172190
                    File.SetCreationTime(actualFileName, currentDateTime);
                    File.Move(actualFileName, archiveFileName);
                }
                catch (IOException)
                {
                    // catch errors and attempt move to a new file with a GUID
                    archiveFileName = archiveFileName + Guid.NewGuid().ToString();
                    try
                    {
                        File.Move(actualFileName, archiveFileName);
                    }
                    catch (IOException) { }
                }
            }

            private void PurgeArchivedFiles(string actualFileName)
            {
                if (this.owner.maxArchivedFiles > 0)
                {
                    var directoryName = Path.GetDirectoryName(actualFileName);
                    var fileName = Path.GetFileName(actualFileName);
                    new RollingFlatFilePurger(directoryName, fileName, this.owner.maxArchivedFiles).Purge();
                }
            }
        }
    }
}
