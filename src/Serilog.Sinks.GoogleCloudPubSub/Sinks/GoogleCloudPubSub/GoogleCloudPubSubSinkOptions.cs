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
        //      CONFIGURABLE EXECUTION OPTIONS
        //*******************************************************************


        #region ------ Google PubSub settings ------------------------

        ///<summary>
        /// GoogleCloudOubSub project to publish.
        /// </summary>
        public string ProjectId{get;set;}

        ///<summary>
        /// GoogleCloudOubSub topic to publish.
        /// </summary>
        public string TopicId{get;set;}

        #endregion


        #region ------ Common (durable and periodic) settings ------------------------

        ///<summary>
        /// The maximum number of events to post in a single batch.
        /// </summary>
        public int BatchPostingLimit { get; set; }

        /// <summary>
        /// The minimum log event level required in order to write an event to the sink.
        /// </summary>
        public LogEventLevel? MinimumLogEventLevel { get; set; }

        /// <summary>
        ///  Customizes the formatter used when converting events into data to send to PubSub.
        /// </summary>
        public ITextFormatter CustomFormatter { get; set; }

        #endregion


        #region ------ Periodic Batching settings ------------------------

        ///<summary>
        /// The time to wait between checking for event batches. Defaults to 2 seconds.
        /// </summary>
        public TimeSpan Period { get; set; }

        #endregion


        #region ------ Durable settings (using buffer file on disk) ------------------------

        /// <summary>
        /// The interval between checking the buffer files.
        /// </summary>
        public TimeSpan? BufferLogShippingInterval { get; set; }

        //--- The following settings are related to the internal use of RollingFile Sink to manage buffer files. ---

        /// <summary>
        /// Path to directory that can be used as a log shipping buffer for increasing the reliability of the log forwarding.
        /// </summary>
        public string BufferBaseFilename { get; set; }

        /// <summary>
        /// Extension for the buffer files (will be added to the given BufferBaseFilename).
        /// </summary>
        public string BufferFileExtension { get; set; }

        /// <summary>
        /// The maximum size, in bytes, to which the buffer file for a specific date will be allowed to grow. By default no limit will be applied.
        /// </summary>
        public long? BufferFileSizeLimitBytes { get; set; }

        /// <summary>
        /// The maximum number of buffer files that will be retained, including the current buffer file. For unlimited retention, pass null. The default is 31.
        /// </summary>
        public int? BufferRetainedFileCountLimit { get; set; }

        #endregion


        #region ------ Errors storage settings (using file on disk) ------------------------

        /// <summary>
        /// Path to directory that can be used as a log shipping for storing internal errors.
        /// If set then it means we want to store errors.
        /// It can be used the same path as the buffer log (BufferBaseFilename) but the file name can't start with the same string.
        /// </summary>
        public string ErrorBaseFilename { get; set; }

        /// <summary>
        /// The maximum size, in bytes, to which the error file for a specific date will be allowed to grow. By default no limit will be applied.
        /// </summary>
        public long? ErrorFileSizeLimitBytes { get; set; }

        /// <summary>
        /// If set to 'true' then events related to any error will be saved to the error file (after the error message).
        /// </summary>
        public bool ErrorStoreEvents { get; set; }

        #endregion



        //TODO: Temporally not used: 
        /////<summary>
        ///// Throw LoggingException if  error publishing messages
        ///// </summary>  
        //public bool ThrowPublishExceptions { get; set; }




        //*******************************************************************
        //      CONSTRUCTORS
        //*******************************************************************

        #region

        /// <summary>
        /// Configures the GoogleCloudPubSub sink defaults.
        /// </summary>
        protected GoogleCloudPubSubSinkOptions()
        {
            //--- Constructor with not null/zero default values -----------------

            this.BatchPostingLimit = 50; 
            this.CustomFormatter = new GoogleCloudPubSubRawFormatter();     // Default formatter: raw data.
            //TODO: Temporally not used: this.ThrowPublishExceptions = true;
            this.BufferFileExtension = ".csv";
            this.BufferLogShippingInterval = TimeSpan.FromSeconds(2);
            this.Period = TimeSpan.FromSeconds(2);
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




        //*******************************************************************
        //      PUBLIC
        //*******************************************************************

        #region

        public void SetValues(
            string bufferBaseFilename,
            long? bufferFileSizeLimitBytes = null,
            int? bufferLogShippingIntervalMilisec = null,
            int? bufferRetainedFileCountLimit = null,
            string bufferFileExtension = null,
            int? batchPostingLimit = null,
            LogEventLevel minimumLogEventLevel = LevelAlias.Minimum,
            string errorBaseFilename = null,
            long? errorFileSizeLimitBytes = null,
            bool? errorStoreEvents = null)
        {
            this.BufferBaseFilename = bufferBaseFilename;
            this.ErrorBaseFilename = errorBaseFilename;

            this.MinimumLogEventLevel = minimumLogEventLevel;

            if (bufferFileSizeLimitBytes != null)
                this.BufferFileSizeLimitBytes = bufferFileSizeLimitBytes.Value;

            if (errorFileSizeLimitBytes != null)
                this.ErrorFileSizeLimitBytes = errorFileSizeLimitBytes.Value;

            if (bufferLogShippingIntervalMilisec != null)
                this.BufferLogShippingInterval = TimeSpan.FromMilliseconds(bufferLogShippingIntervalMilisec.Value);

            if (bufferRetainedFileCountLimit != null)
                this.BufferRetainedFileCountLimit = (bufferRetainedFileCountLimit.Value < 2 ? 2 : bufferRetainedFileCountLimit.Value);

            if (!string.IsNullOrEmpty(bufferFileExtension))
                this.BufferFileExtension = bufferFileExtension;

            if (batchPostingLimit != null)
                this.BatchPostingLimit = batchPostingLimit.Value;

            if (errorStoreEvents != null)
                this.ErrorStoreEvents = errorStoreEvents.Value;

        }


        #endregion
    }
}
