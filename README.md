# enterprise-library-tracelisteners
Enhanced tracelisteners for Microsoft Enterprise Library logging v6.  
Addresses some internal issues of Trace Listener impelementations included in enterpise library v6.

## RollingFileTraceListener
###Another take on Microsoft's RollingFlatFileTraceListener included in Enterprise Library.  

One common way to configure the RollingFlatFileTraceListener is to roll over the file at midnight. With the date specified in the file name of the log file, the expectation is that each file will have the day's worth of logged entries for the date indicated in the filename.

Due to a bug (feature) in the Microsoft implementation, the date used in the file names does not indicate the date of the entries contained within the log file.

In Microsoft's implementation, the date in the filename indicates the date the file rollover occurred.  At best with the rollover happening right after midnight, the date was always off by one day. 

If there were no loggable events on a given day, the discrepancy in dates grows. This can pose much confusion and a waste of valuable time when going back to logs files and trying to find logging information for an event that happened on a known date.

###Introducing RollingFileTraceListener
This implementation addresses the filename's date issue by using the date the file was created when rolling and naming the file with a date. 

Other features present in Microsoft's implementation such as `Header`, `Footer`, `Formatter`, `FileName`, `RollFileExistsBehavior`, `RollInterval`, `RollSizeKB`, `TimeStampPattern`, and `MaxArchivedFiles` are supported.

Other features documented [Here (MSDN)](https://msdn.microsoft.com/en-us/library/microsoft.practices.enterpriselibrary.logging.configuration.rollingflatfiletracelistenerdata_properties.aspx)

## Configuring RollingFileTraceListener
In your program's configuration file, configuring this tracelistener is nearly identical to Microsoft's RollingFlatFileTraceListener and support the same options.

``` XML
<loggingConfiguration tracingEnabled="true" name="TraceLog" defaultCategory="Information">
  <listeners>
    <add name="TextFile" type="BWSoftInc.EnterpriseLibrary.TraceListeners.RollingFileTraceListener, BWSoftInc.EnterpriseLibrary.TraceListeners"
         listenerDataType="BWSoftInc.EnterpriseLibrary.TraceListeners.RollingFileTraceListenerData, BWSoftInc.EnterpriseLibrary.TraceListeners"
         fileName="myapplication.log" 
         rollInterval="Midnight" 
         rollFileExistsBehavior="Increment" 
         rollSizeKB="0" header="" footer="" maxArchivedFiles="30" formatter="Message" />
  </listeners>
  <formatters>
    <add name="Message" type="Microsoft.Practices.EnterpriseLibrary.Logging.Formatters.TextFormatter, Microsoft.Practices.EnterpriseLibrary.Logging" template="{message}" />
  </formatters>
  <categorySources>
  <add switchValue="All" name="Error">
    <listeners>
      <add name="TextFile" />
    </listeners>
  </add>
  </categorySources>
  <specialSources>
    <errors switchValue="All" name="errors" />
    <allEvents switchValue="All" name="allEvents" />
    <notProcessed switchValue="All" name="notProcessed" />
  </specialSources>
</loggingConfiguration>
```

## License

Copyright (c) 2016 Brian Waplington

Copyright (c) 2016 BW Soft, Inc.

[MIT License](https://raw.githubusercontent.com/bwsoftinc/enterprise-library-tracelisteners/master/LICENSE)
