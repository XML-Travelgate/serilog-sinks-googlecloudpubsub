// Copyright 2014 Serilog Contributors
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.RollingFile;


namespace Serilog.Sinks.GoogleCloudPubSub
{
     class DurableGoogleCloudPubSubSink : ILogEventSink, IDisposable
    {
        
        // we rely on the date in the filename later!
        const string FileNameSuffix = "-{Date}.json";
        readonly RollingFileSink _sink;
        readonly GoogleCloudPubSubLogShipper _shipper;
        readonly GoogleCloudPubSubSinkState _state;
   
        public DurableGoogleCloudPubSubSink(GoogleCloudPubSubSinkOptions options)
        {
            _state = GoogleCloudPubSubSinkState.Create(options);
           
            if (string.IsNullOrWhiteSpace(options.BufferBaseFilename)){
                throw new ArgumentException("Cannot create the durable GoogleCloudPubSub sink without BufferBaseFilename");
            }

            if ( !options.BufferLogShippingInterval.HasValue ){
                throw new ArgumentException("Cannot create the durable GoogleCloudPubSub sink without BufferLogShippingInterval");
            }

           _sink = new RollingFileSink(
                    options.BufferBaseFilename + FileNameSuffix,
                    _state.DurableFormatter,
                    options.BufferFileSizeLimitBytes,
                    options.BufferRetainedFileCountLimit
                );
            
            _shipper = new GoogleCloudPubSubLogShipper(_state);
        }
    
        public void Emit(LogEvent logEvent)
        {
            _sink.Emit(logEvent);
        }

         public void Dispose()
        {
            _sink.Dispose();
            _shipper.Dispose();
        }
    }

}
