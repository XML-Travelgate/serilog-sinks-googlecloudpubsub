# serilog-sinks-googlecloudpubsub
[![Build status](https://ci.appveyor.com/api/projects/status/afbwe9ssan2quind?svg=true)](https://ci.appveyor.com/project/operezfuentes/serilog-sinks-googlecloudpubsub/branch/master)  [![NuGet Version](http://img.shields.io/nuget/v/Serilog.Sinks.GoogleCloudPubSub.svg?style=flat)](https://www.nuget.org/packages/Serilog.Sinks.GoogleCloudPubSub/) [![Rager Releases](http://rager.io/badge.svg?url=https%3A%2F%2Fwww.nuget.org%2Fpackages%2FSerilog.Sinks.GoogleCloudPubSub%2F)](http://rager.io/projects/search?badge=1&query=nuget.org/packages/Serilog.Sinks.GoogleCloudPubSub/)

A Serilog sink that writes events to Google Cloud Pub Sub

Branch  | AppVeyor 
------------- | ------------- 
dev | [![Build status](https://ci.appveyor.com/api/projects/status/afbwe9ssan2quind/branch/dev?svg=true)](https://ci.appveyor.com/project/operezfuentes/serilog-sinks-googlecloudpubsub/branch/dev) 
master | [![Build status](https://ci.appveyor.com/api/projects/status/afbwe9ssan2quind/branch/master?svg=true)](https://ci.appveyor.com/project/operezfuentes/serilog-sinks-googlecloudpubsub/branch/master)


# Implementation notes

- Based on https://github.com/serilog/serilog-sinks-seq and https://github.com/serilog/serilog-sinks-elasticsearch
- Internally uses performing grpc communication via Google.Pubsub.V1
- Durable (via RollingFile) and/or PeriodicBatching sink configuration

# Notes 

When using google-cloud-dotnet libraries elsewhere, you can do one of the following:

- Define the environment variable GOOGLEAPPLICATIONCREDENTIALS to be the location of the key. For example:
set GOOGLE_APPLICATION_CREDENTIALS=/path/to/my/key.json

- If running locally for development/testing, you can authenticate using the Google Cloud SDK. Download the SDK if you haven't already, then login by running the following in the command line:
gcloud auth login
