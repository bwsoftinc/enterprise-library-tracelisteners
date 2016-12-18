using System.Diagnostics;
using System.Configuration;
using Microsoft.Practices.EnterpriseLibrary.Common.Configuration;
using Microsoft.Practices.EnterpriseLibrary.Logging.Configuration;
using Microsoft.Practices.EnterpriseLibrary.Logging.TraceListeners;
using Microsoft.Practices.EnterpriseLibrary.Common.Configuration.Design;

namespace BWSoftInc.EnterpriseLogging.TraceListeners
{ 
    public class RollingFileTraceListenerData : TraceListenerData    {
        private const string FileNamePropertyName = "fileName";
        private const string footerProperty = "footer";
        private const string formatterNameProperty = "formatter";
        private const string headerProperty = "header";
        private const string RollFileExistsBehaviorPropertyName = "rollFileExistsBehavior";
        private const string RollIntervalPropertyName = "rollInterval";
        private const string RollSizeKBPropertyName = "rollSizeKB";
        private const string TimeStampPatternPropertyName = "timeStampPattern";
        private const string MaxArchivedFilesPropertyName = "maxArchivedFiles";
                  
        public RollingFileTraceListenerData()
            : base(typeof(RollingFlatFileTraceListener))
        {
            ListenerDataType = typeof(RollingFlatFileTraceListenerData);
        }

        public RollingFileTraceListenerData(string name,
            string fileName,
            string header,
            string footer,
            int rollSizeKB,
            string timeStampPattern,
            RollFileExistsBehavior rollFileExistsBehavior,
            RollInterval rollInterval,
            TraceOptions traceOutputOptions,
            string formatter)
            : base(name, typeof(RollingFlatFileTraceListener), traceOutputOptions)
        {
            FileName = fileName;
            Header = header;
            Footer = footer;
            RollSizeKB = rollSizeKB;
            RollFileExistsBehavior = rollFileExistsBehavior;
            RollInterval = rollInterval;
            TimeStampPattern = timeStampPattern;
            Formatter = formatter;
       }

       public RollingFileTraceListenerData(string name,
           string fileName,
           string header,
           string footer,
           int rollSizeKB,
           string timeStampPattern,
           RollFileExistsBehavior rollFileExistsBehavior,
           RollInterval rollInterval,
           TraceOptions traceOutputOptions,
           string formatter,
           SourceLevels filter)
            : base(name, typeof(RollingFlatFileTraceListener), traceOutputOptions, filter)
        {
            FileName = fileName;
            Header = header;
            Footer = footer;
            RollSizeKB = rollSizeKB;
            RollFileExistsBehavior = rollFileExistsBehavior;
            RollInterval = rollInterval;
            TimeStampPattern = timeStampPattern;
            Formatter = formatter;
        }      

        [ConfigurationProperty(FileNamePropertyName, DefaultValue = "rolling.log")]
        [System.ComponentModel.Editor(CommonDesignTime.EditorTypes.FilteredFilePath, CommonDesignTime.EditorTypes.UITypeEditor)]
        public string FileName
        {
            get { return (string)this[FileNamePropertyName]; }
            set { this[FileNamePropertyName] = value; }
        }        

        [ConfigurationProperty(footerProperty, IsRequired = false, DefaultValue = "----------------------------------------")]
        public string Footer
        {
            get { return (string)base[footerProperty]; }
            set { base[footerProperty] = value; }
        }  

        [ConfigurationProperty(formatterNameProperty, IsRequired = false)]
        [Reference(typeof(NameTypeConfigurationElementCollection<FormatterData, CustomFormatterData>), typeof(FormatterData))]
        public string Formatter
        {
            get { return (string)base[formatterNameProperty]; }
            set { base[formatterNameProperty] = value; }
        }      

        [ConfigurationProperty(headerProperty, IsRequired = false, DefaultValue = "----------------------------------------")]
        public string Header
        {
            get { return (string)base[headerProperty]; }
            set { base[headerProperty] = value; }
        }      

        [ConfigurationProperty(RollFileExistsBehaviorPropertyName)]
        public RollFileExistsBehavior RollFileExistsBehavior
        {
            get { return (RollFileExistsBehavior)this[RollFileExistsBehaviorPropertyName]; }
            set { this[RollFileExistsBehaviorPropertyName] = value; }
        }      

        [ConfigurationProperty(RollIntervalPropertyName)]
        public RollInterval RollInterval
        {
            get { return (RollInterval)this[RollIntervalPropertyName]; }
            set { this[RollIntervalPropertyName] = value; }
        }     

        [ConfigurationProperty(RollSizeKBPropertyName)]   
        public int RollSizeKB
        {
            get { return (int)this[RollSizeKBPropertyName]; }
            set { this[RollSizeKBPropertyName] = value; }
        }        

        [ConfigurationProperty(TimeStampPatternPropertyName, DefaultValue = "yyyy-MM-dd")]       
        public string TimeStampPattern
        {
            get { return (string)this[TimeStampPatternPropertyName]; }
            set { this[TimeStampPatternPropertyName] = value; }
        }       

        [ConfigurationProperty(MaxArchivedFilesPropertyName)]
        public int MaxArchivedFiles
        {
            get { return (int)this[MaxArchivedFilesPropertyName]; }
            set { this[MaxArchivedFilesPropertyName] = value; }
        } 

        protected override TraceListener CoreBuildTraceListener(LoggingSettings settings)
        {
            var formatter = this.BuildFormatterSafe(settings, this.Formatter);
            return new RollingFileTraceListener(
                this.FileName,
                this.Header,
                this.Footer, 
                formatter,  
                this.RollSizeKB,      
                this.TimeStampPattern, 
                this.RollFileExistsBehavior, 
                this.RollInterval,  
                this.MaxArchivedFiles);
        }
    }
}