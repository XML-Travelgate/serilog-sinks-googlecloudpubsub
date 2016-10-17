# Serilog-Sinks-GoogleCloudPubsub
[![Build status](https://ci.appveyor.com/api/projects/status/afbwe9ssan2quind?svg=true)](https://ci.appveyor.com/project/operezfuentes/serilog-sinks-googlecloudpubsub/branch/master)  [![NuGet Version](http://img.shields.io/nuget/v/Serilog.Sinks.GoogleCloudPubSub.svg?style=flat)](https://www.nuget.org/packages/Serilog.Sinks.GoogleCloudPubSub/) [![Rager Releases](http://rager.io/badge.svg?url=https%3A%2F%2Fwww.nuget.org%2Fpackages%2FSerilog.Sinks.GoogleCloudPubSub%2F)](http://rager.io/projects/search?badge=1&query=nuget.org/packages/Serilog.Sinks.GoogleCloudPubSub/)

A [Serilog](https://serilog.net) sink that writes events to Google Cloud PubSub.

Branch  | AppVeyor 
------------- | ------------- 
dev | [![Build status](https://ci.appveyor.com/api/projects/status/afbwe9ssan2quind/branch/dev?svg=true)](https://ci.appveyor.com/project/operezfuentes/serilog-sinks-googlecloudpubsub/branch/dev) 
master | [![Build status](https://ci.appveyor.com/api/projects/status/afbwe9ssan2quind/branch/master?svg=true)](https://ci.appveyor.com/project/operezfuentes/serilog-sinks-googlecloudpubsub/branch/master)



# Implementation notes

- Based on [Serilog.Sinks.Seq] (https://github.com/serilog/serilog-sinks-seq) and [Serilog.Sinks.Elasticsearch] (https://github.com/serilog/serilog-sinks-elasticsearch)
- Internally uses performing grpc communication via [Google.Pubsub.V1] (https://github.com/GoogleCloudPlatform/google-cloud-dotnet/tree/master/apis/Google.Pubsub.V1)
- Internally uses durable sink configuration via [Serilog.Sinks.RollingFile] (https://github.com/serilog/serilog-sinks-rollingfile/) . 
- PeriodicBatching not implemented yet.



# Google PubSub Authentication

It is used the authentication specified by the [Google.Pubsub.V1] (https://github.com/GoogleCloudPlatform/google-cloud-dotnet/tree/master/apis/Google.Pubsub.V1) package.
Please refer to its documentation to see how it works. 

The most common method to authenticate is to define the environment variable GOOGLE_APPLICATION_CREDENTIALS to be the location of the key (a .json file).
This environment variable can be set for a machine, for a user or also can be set into code (for the process).
For example: set GOOGLE_APPLICATION_CREDENTIALS=/path/to/my/key.json



# Getting started

Install the [Serilog.Sinks.GoogleCloudPubSub](https://nuget.org/packages/serilog.sinks.googlecloudpubsub) package from NuGet:

```powershell
Install-Package Serilog.Sinks.GoogleCloudPubSub
```


### Creating the logger

To configure the sink in C# code, call `WriteTo.GoogleCloudPubSub()` during logger configuration passing an object with the options you need:

```csharp
var log = new LoggerConfiguration()
    .WriteTo.GoogleCloudPubSub(new GoogleCloudPubSubSinkOptions("_project_", "_topic_")
	{
		BufferBaseFilename = "\\path\\buffer-file-name"
	})
    .CreateLogger();
```

You have to provide the PubSub project and topic to the constructor of the object with the options (GoogleCloudPubSubSinkOptions type) and also you have to set the path and name of the file that will be used as buffer on disk (without extension). 
The other configuration options are all optional and if are not set then default values will be used.

You can also instantiate and initialize the durable sink outside the logger configuration and then provide the instance:

```csharp
DurableGoogleCloudPubSubSink mySinkPubSub = new DurableGoogleCloudPubSubSink(new GoogleCloudPubSubSinkOptions("_project_", "_topic_")
	{
		BufferBaseFilename = "\\path\\buffer-file-name"
	});

var log = new LoggerConfiguration()
    .WriteTo.GoogleCloudPubSub(mySinkPubSub)
    .CreateLogger();
```

In both cases an object of type GoogleCloudPubSubSinkOptions is used to set the configuration options of the sink, which you can instantiate separately too.

```csharp
GoogleCloudPubSubSinkOptions mySinkPubSubOptions = new GoogleCloudPubSubSinkOptions("_project_", "_topic_")
	{
		BufferBaseFilename = "\\path\\buffer-file-name"
	};
	
var log = new LoggerConfiguration()
    .WriteTo.GoogleCloudPubSub(mySinkPubSubOptions)
    .CreateLogger();
```

And you can also provide the configuration values directly while configurating the logger:

```csharp
var log  = new LoggerConfiguration()
	.WriteTo.GoogleCloudPubSub("_project_", "_topic_", "\\path\\buffer-file-name")
	.CreateLogger();
```

In all cases the project, the topic and the path for buffer file on disk are mandatory.
The other configuration options are all optional and if are not set then default values will be used.


### Sending data

To send an event just call the method associated with the level you desire:

```csharp
Log.Information("This will be sent to Google PubSub");
```

The default formatter (GoogleCloudPubSubRawFormatter) will send to PubSub the messageTemplate you define and will ignore any parameter you set for the event.



# Basic configuration options


### ProjectId
Google PubSub project ID (mandatory).

### TopicId
Google PubSub topic ID (mandatory).

### BufferBaseFilename
Path (mandatory) for the buffer file on disk.
It must not contain any rolling specifier neither the file extension.

### BatchPostingLimit
The maximum number of events to post to PubSub in a single batch.
The default value is 50.

### BatchSizeLimitBytes
The maximum size, in bytes, to post to PubSub in a single batch.
By default no limit will be applied.

### MinimumLogEventLevel
The minimum log event level required in order to write an event to the sink.

### CustomFormatter
Customizes the formatter used when converting events into data to send to PubSub.

### BufferLogShippingInterval
The interval (miliseconds) between checking the buffer files.
The default value is 2 seconds.

### BufferFileExtension
Extension for the buffer files (will be added to the given BufferBaseFilename).
The default value is ".swap".

### BufferRollingSpecifier
Rolling specifier: {Date}, {Hour} or {HalfHour}.
The default one is {Hour}.

### BufferFileSizeLimitBytes
The maximum size, in bytes, to which the buffer file for the specifier will be allowed to grow.
By default no limit will be applied.

### BufferRetainedFileCountLimit
The maximum number of buffer files that will be retained, including the current buffer file. For unlimited retention, pass null.
The default value is 31.



# Data configuration options

### MessageDataToBase64
If set to 'true' then data on PubSub messages is converted to Base64.
By default it is true.

### EventFieldSeparator
Fields separator in event data.

### MessageAttrMinValue
If given indicates that the PubSub message has to contain an attribute that is obtained as the MIN value for a concret field in the event dada.
This value has to be the field position (0 base), the separator "#" and the name to give to the PubSub message attribute.
It is mandatory to specify the fields separador with the property EventFieldSeparator.
If there is any problem then no attribute will be added to the message.
The field from where to get the MIN value will be treated as an string. Null values will be omitted.
Example: "3#timestamp"
		
### MessageAttrFixed
If given then in each message to PubSub will be added as many attributes as elements has de dictionary, where the key corresponds to an attribute name and the value corresponds to its value to set.



# Errors and debug configuration options


### ErrorBaseFilename
Path that can be used as a log for storing internal errors and debuf information.
If set then it means we want to store errors and/or debug information.
It can be used the same path as the buffer log (BufferBaseFilename) but the file name can't start with the same string.
It must not contain any rolling specifier: it will use the same one set for the buffer file.
By default just errors information is stored.

### ErrorFileSizeLimitBytes
The maximum size, in bytes, to which the error/debug file for the specifier will be allowed to grow.
By default no limit will be applied.

### ErrorStoreEvents
If set to 'true' then events related to any error will be saved to the error file (after the error message).
By default it is false.

### DebugStoreBatchLimitsOverflows
If set to 'true' then overflows when creating batch posts will be stored (overflows for BatchPostingLimit and also for BatchSizeLimitBytes).
By default it is false.

### DebugStoreEventSkip
If set to 'true' then skiped events (greater than the BatchSizeLimitBytes) will be stored.
By default it is false.

### DebugStoreAll
If set to 'true' then ALL debug data will be stored. If set to 'false' then each type of debug data will be stored depending on its own switch.
By default it is false.	

### ErrorRetainedFileCountLimit
The maximum number of error log files that will be retained, including the current error file. For unlimited retention, pass null.
The default value is 31.



# Performance


### Writing to buffer file on disk

By default, this sink will flush each event written through it to the buffer file on disk and it will be sent to PubSub from disk when a timer expires.
The `buffered: true` functionality of the underlying RollingFile is not exposed on this sink as it doesn't ensure that each event is completely written to disk (they can be written partialy).

The [Serilog.Sinks.Async](https://github.com/serilog/serilog-sinks-async) package can be used to wrap this sink and perform all disk writings on a background worker thread.

```csharp
DurableGoogleCloudPubSubSink mySinkPubSub = new DurableGoogleCloudPubSubSink(new GoogleCloudPubSubSinkOptions("_project_", "_topic_")
	{
		BufferBaseFilename = "\\path\\buffer-file-name"
	});

var log = new LoggerConfiguration()
    .WriteTo.Async(a => a.GoogleCloudPubSub(mySinkPubSub))
    .CreateLogger();
```

```csharp
GoogleCloudPubSubSinkOptions mySinkPubSubOptions = new GoogleCloudPubSubSinkOptions("_project_", "_topic_")
	{
		BufferBaseFilename = "\\path\\buffer-file-name"
	};
	
var log = new LoggerConfiguration()
    .WriteTo.Async(a => a.GoogleCloudPubSub(mySinkPubSubOptions))
    .CreateLogger();
```


### Formatting data to be sent to PubSub

Serilog works with structured log events and the messageTemplate is examined char by char to find parameter references. This means that if we use a large messageTemplate then it will be spent some time looking for parameters.
To avoid to spend this time we can use a short messageTemplate and then define a parameter with the real data to send to PubSub: in this case the default formatter (GoogleCloudPubSubRawFormatter) will not do the proper work and we have to use the GoogleCloudPubSubParamFormatter formatter.

```csharp
DurableGoogleCloudPubSubSink mySinkPubSub = new DurableGoogleCloudPubSubSink(new GoogleCloudPubSubSinkOptions("_project_", "_topic_")
	{
		BufferBaseFilename = "\\path\\buffer-file-name",
		CustomFormatter = new GoogleCloudPubSubParamFormatter()
	});

var log = new LoggerConfiguration()
    .WriteTo.Async(a => a.GoogleCloudPubSub(mySinkPubSub))
    .CreateLogger();
```

```csharp
GoogleCloudPubSubSinkOptions mySinkPubSubOptions = new GoogleCloudPubSubSinkOptions("_project_", "_topic_")
	{
		BufferBaseFilename = "\\path\\buffer-file-name",
		CustomFormatter = new GoogleCloudPubSubParamFormatter()
	};
	
var log = new LoggerConfiguration()
    .WriteTo.Async(a => a.GoogleCloudPubSub(mySinkPubSubOptions))
    .CreateLogger();
```

And then:

```csharp
//Avoiding to spend time with a large messageTemplate: we set the message as a parameter.
Log.Information("{mt}", "This will be sent to Google PubSub");
```

For compatibility: with GoogleCloudPubSubParamFormatter formatter we can also log events using directly the messageTemplate as the message and not setting parameters.

```csharp
//This also works with GoogleCloudPubSubParamFormatter
//but we will spend time searching for not defined parameter references.
Log.Information("This will be sent to Google PubSub");
```








