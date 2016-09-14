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
using Serilog.Formatting;
using Serilog.Events;



namespace Serilog.Sinks.GoogleCloudPubSub
{
    /// <summary>
    /// Provides GoogleCloudPubSubSink with configurable options
    /// </summary>
    public class GoogleCloudPubSubSinkOptions
    {


        //*******************************************************************
        //      CONFIGURABLE EXECUTING OPTIONS
        //*******************************************************************

        #region
        ///<summary>
        /// GoogleCloudOubSub project to publish.
        /// </summary>
        public string ProjectId{get;set;}

        ///<summary>
        /// GoogleCloudOubSub topic to publish.
        /// </summary>
        public string TopicId{get;set;}

        ///<summary>
        /// The maximum number of events to post in a single batch.
        /// </summary>
        public int BatchSizeLimit { get; set; }

        ///<summary>
        /// The time to wait between checking for event batches. Defaults to 2 seconds.
        /// </summary>
        public TimeSpan Period { get; set; }
        
        ///<summary>
        /// Throw LoggingException if  error publishing messages
        /// </summary>  
        public bool ThrowPublishExceptions { get; set; }
        /// <summary>
        ///  Customizes the formatter used when converting log events into data to send to PubSub.
        /// </summary>
        public ITextFormatter CustomFormatter { get; set; }

        /// <summary>
        /// Optional path to directory that can be used as a log shipping buffer for increasing the reliability of the log forwarding.
        /// </summary>
        public string BufferBaseFilename { get; set; }

        /// <summary>
        /// The maximum size, in bytes, to which the buffer log file for a specific date will be allowed to grow. By default no limit will be applied.
        /// </summary>
        public long? BufferFileSizeLimitBytes { get; set; }

        /// <summary>
        /// Extension for the buffer files.
        /// </summary>
        public string BufferFileExtension { get; set; }

        /// <summary>
        /// The interval between checking the buffer files
        /// </summary>
        public TimeSpan? BufferLogShippingInterval { get; set; }

        /// <summary>
        /// The interval between checking the buffer files
        /// </summary>
        public int? BufferRetainedFileCountLimit { get; set; }

        /// <summary>
        /// The minimum log event level required in order to write an event to the sink.
        /// </summary>
        public LogEventLevel? MinimumLogEventLevel { get; set; }
        #endregion



        //*******************************************************************
        //      CONSTRUCTORS
        //*******************************************************************

        #region

        /// <summary>
        /// Configures the GoogleCloudPubSub sink defaults.
        /// </summary>
        protected GoogleCloudPubSubSinkOptions()
        {
            this.Period = TimeSpan.FromSeconds(10);
            this.BatchSizeLimit = 50; 
            this.CustomFormatter = new GoogleCloudPubSubRawFormatter();     // Default formatter: raw data.
            this.ThrowPublishExceptions = true;
            this.Period = TimeSpan.FromSeconds(2);
            this.BufferFileExtension = ".csv";
        }

        /// <summary>
        /// Configures the GoogleCloudPubSub sink with parameters.
        /// </summary>
        public GoogleCloudPubSubSinkOptions(string projectId, string topicId) : this()
        {
            this.ProjectId = projectId;
            this.TopicId = topicId;
        }
        #endregion

    }
}
