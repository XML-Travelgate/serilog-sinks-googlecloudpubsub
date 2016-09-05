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
    // This class uses a file on disk as a buffer previous sending data (in blocks) to the remote server.
    // It is used RollingFileSink to manage this buffer file.

    class DurableGoogleCloudPubSubSink : ILogEventSink, IDisposable
    {

        //*******************************************************************
        //      PRIVATE FIELDS
        //*******************************************************************

        #region
        // Sufix for the file used with rolling file sink.
        // The main name and the extension are set in the options.
        const string FileNameSuffix = "-{Date}";

        // Google Cloud PubSub Shipper and State instances.
        readonly GoogleCloudPubSubSinkState _state;     // -> Contains the options.
        readonly GoogleCloudPubSubLogShipper _shipper;  // -> Contains the State and extracts and uses some options.

        // RollingFileSink instance to manage the buffer file.
        readonly RollingFileSink _rollingFileSink;
        #endregion




        //*******************************************************************
        //      CONSTRUCTOR
        //*******************************************************************

        #region
        public DurableGoogleCloudPubSubSink(GoogleCloudPubSubSinkOptions options)
        {
            //--- Mandatory options validations --------------------
            if (string.IsNullOrWhiteSpace(options.BufferBaseFilename)){
                throw new ArgumentException("Cannot create the durable GoogleCloudPubSub sink without BufferBaseFilename");
            }

            if (!options.BufferLogShippingInterval.HasValue ){
                throw new ArgumentException("Cannot create the durable GoogleCloudPubSub sink without BufferLogShippingInterval");
            }

            //---
            // All is ok ... instances are created using the defined options...

            this._state = GoogleCloudPubSubSinkState.Create(options);
            this._shipper = new GoogleCloudPubSubLogShipper(this._state);

            this._rollingFileSink = new RollingFileSink(
                    options.BufferBaseFilename + FileNameSuffix + options.BufferFileExtension,
                    this._state.DurableFormatter,   // Formatter for data to insert into the buffer file.
                    options.BufferFileSizeLimitBytes,
                    options.BufferRetainedFileCountLimit
                );
        }
        #endregion




        //*******************************************************************
        //      ILogEventSink
        //*******************************************************************

        #region
        public void Emit(LogEvent logEvent)
        {
            // This method is executed each time we write anything on the Serilog instance with this sink.
            // Log event is formatted using the assigned formatter (by default it is GoogleCloudPubSubRawFormatter)
            // and then the result string is stored into the buffer file that is managed by the RollingFileSink.
            this._rollingFileSink.Emit(logEvent);
        }
        #endregion



        //*******************************************************************
        //      IDisposable
        //*******************************************************************

        #region
        public void Dispose()
        {
            if (this._rollingFileSink != null)
            {
                this._rollingFileSink.Dispose();
            }

            if (this._shipper != null)
            {
                this._shipper.Dispose();
            }
        }
        #endregion

    }

}
