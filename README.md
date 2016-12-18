# enterprise-library-tracelisteners
Enhanced tracelisteners for Microsoft Enterprise Library logging v6.  
Addresses some internal issues of Trace Listener impelementations included in enterpise library v6.

## RollingFileTraceListener
###Another take on Microsoft's RollingFlatFileTraceListener included in Enterprise Library.  

One common way to configure the RollingFlatFileTraceListener is to roll over the file at midnight. With the date specified in the file name of the log file, the expectation is that each file will have the day's worth of logged entries for the date indicated on the filename.

Due to a bug (feature) in the Microsoft implementation, the date used in the file names does not indicate the date of the entries contained within the log file.

In Microsoft's impelementation, the date in the filename indicates the date the file rollover occurred.  At best with the rollover occurring right after midnight, the date was always off by one day. 

If there were no loggable events on a given day, the discrepancy in dates grows. This can pose much confusion and a waste of valuable time When going back to logs files and trying to find logging information for an event that happened on a known date.

###Introducing RollingFileTraceListener
This implementation addresses that issue by using the date the file was created when rolling and naming the file with a date.
Other features present in Microsoft's implementation such as Header, Footer, Formatter, FileName, RollFileExistsBehavior, RollInterval, RollSizeKB, TimeStampPattern, and MaxArchivedFiles are supported and are documented [Here (MSDN)](https://msdn.microsoft.com/en-us/library/microsoft.practices.enterpriselibrary.logging.configuration.rollingflatfiletracelistenerdata_properties.aspx)

## Configuring RollingFileTraceListener
In your program's configuration file, configuring this tracelistener is nearly identical to Microsoft's RollingFlatFileTraceListener and support the same options.

Coming Soon!


## License

Copyright (c) 2016 Brian Waplington

Copyright (c) 2016 BW Soft, Inc.

[MIT License](https://raw.githubusercontent.com/bwsoftinc/enterprise-library-tracelisteners/master/LICENSE)
