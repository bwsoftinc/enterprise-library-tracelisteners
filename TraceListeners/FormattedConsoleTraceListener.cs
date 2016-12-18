using System;
using System.Diagnostics;
using Microsoft.Practices.EnterpriseLibrary.Logging;
using Microsoft.Practices.EnterpriseLibrary.Common.Configuration;
using Microsoft.Practices.EnterpriseLibrary.Logging.Configuration;
using Microsoft.Practices.EnterpriseLibrary.Logging.TraceListeners;

namespace BWSoftInc.EnterpriseLogging.TraceListeners
{
    [ConfigurationElementType(typeof(CustomTraceListenerData))]
    public class FormattedConsoleTraceListener : CustomTraceListener
    {
        public override void Write(string message)
        {
            Console.Write(message);
        }

        public override void WriteLine(string message)
        {
            Console.WriteLine(message);
        }

        public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, object data)
        {
            var entry = data as LogEntry;

            if (entry != null)
            {
                if (Formatter != null)
                    WriteLine(Formatter.Format(entry));
                else
                    WriteLine(entry.Message);
            }
            else
            {
                base.TraceData(eventCache, source, eventType, id, data);
            }
        }
    }
}
