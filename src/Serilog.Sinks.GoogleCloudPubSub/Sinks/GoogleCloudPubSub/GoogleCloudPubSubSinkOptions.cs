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
using System.Collections.Generic;
using Serilog.Sinks.GoogleCloudPubSub.Formatters;

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
        /// GoogleCloudPubSub project to publish.
        /// </summary>
        public string ProjectId{get;set;}

        ///<summary>
        /// GoogleCloudOubSub topic to publish.
        /// </summary>
        public string TopicId{get;set;}

        #endregion


        #region ------ Common (durable and periodic) settings ------------------------

        ///<summary>
        /// The maximum number of events to post to PubSub in a single batch.
        /// </summary>
        public int BatchPostingLimit { get; set; }

        ///<summary>
        /// The maximum size, in bytes, to post to PubSub in a single batch. By default no limit will be applied.
        /// </summary>
        public long? BatchSizeLimitBytes { get; set; }

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


        #region ------ Durable Batching settings (using buffer file on disk) ------------------------

        /// <summary>
        /// The interval (miliseconds) between checking the buffer files.
        /// </summary>
        public TimeSpan? BufferLogShippingInterval { get; set; }

        //--- The following settings are related to the internal use of RollingFile Sink to manage buffer files. ---

        /// <summary>
        /// Path to directory that can be used as a log shipping buffer for increasing the reliability of the log forwarding.
        /// It must not contain any rolling specifier.
        /// </summary>
        public string BufferBaseFilename { get; set; }

        /// <summary>
        /// Extension for the buffer files (will be added to the given BufferBaseFilename).
        /// </summary>
        public string BufferFileExtension { get; set; }

        /// <summary>
        /// Rolling specifier: {Date}, {Hour} or {HalfHour}. The default one is {Hour}.
        /// </summary>
        public string BufferRollingSpecifier { get; set; }

        /// <summary>
        /// The maximum size, in bytes, to which the buffer file for the specifier will be allowed to grow. By default no limit will be applied.
        /// </summary>
        public long? BufferFileSizeLimitBytes { get; set; }

        /// <summary>
        /// The maximum number of buffer files that will be retained, including the current buffer file. For unlimited retention, pass null. The default is 31.
        /// </summary>
        public int? BufferRetainedFileCountLimit { get; set; }

        #endregion


        #region ------ Errors and Debug storage settings (using file on disk) ------------------------

        /// <summary>
        /// Path to directory that can be used as a log for storing internal errors and debuf information.
        /// If set then it means we want to store errors and/or debug information.
        /// It can be used the same path as the buffer log (BufferBaseFilename) but the file name can't start with the same string.
        /// It must not contain any rolling specifier: it will use the same one set for the buffer file.
        /// </summary>
        public string ErrorBaseFilename { get; set; }

        /// <summary>
        /// The maximum size, in bytes, to which the error/debug file for the specifier will be allowed to grow. By default no limit will be applied.
        /// </summary>
        public long? ErrorFileSizeLimitBytes { get; set; }

        /// <summary>
        /// If set to 'true' then events related to any error will be saved to the error file (after the error message).
        /// </summary>
        public bool ErrorStoreEvents { get; set; } = false;

        /// <summary>
        /// If set to 'true' then overflows when creating batch posts will be stored (overflows for BatchPostingLimit and also for BatchSizeLimitBytes).
        /// </summary>
        public bool DebugStoreBatchLimitsOverflows { get; set; } = false;

        /// <summary>
        /// If set to 'true' then skiped events (greater than the BatchSizeLimitBytes) will be stored.
        /// </summary>
        public bool DebugStoreEventSkip { get; set; } = false;

        /// <summary>
        /// If set to 'true' then ALL debug data will be stored. If set to 'false' then each type of
        /// debug data will be stored depending on its own switch.
        /// </summary>
        public bool DebugStoreAll { get; set; } = false;

        /// <summary>
        /// The maximum number of error log files that will be retained, including the current error file. For unlimited retention, pass null. The default is 31.
        /// </summary>
        public int? ErrorRetainedFileCountLimit { get; set; }

        /// <summary>
        /// If set to 'true' then all file actions (move forward, delete, ...) will me stored.
        /// </summary>
        public bool DebugStoreFileAction { get; set; } = false;

        #endregion



        #region ------ Settings for data management ------------------------

        /// <summary>
        /// If set to 'true' then data on PubSub messages is converted to Base64. The default value is 'true'.
        /// </summary>
        public bool MessageDataToBase64 { get; set; }

        /// <summary>
        /// Fields separator in event data.
        /// </summary>
        public string EventFieldSeparator { get; set; }

        /// <summary>
        /// If given indicates that the PubSub message has to contain an attribute that is obtained as the MIN value for a concret field in the event dada.
        /// This value has to be the field position (0 base), the separator "#" and the name to give to the PubSub message attribute.
        /// It is mandatory to specify the fields separador with the property EventFieldSeparator.
        /// If there is any problem then no attribute will be added to the message.
        /// The fiel where to get the MIN value will be treated as an string. Null values will be omitted.
        /// </summary>
        public string MessageAttrMinValue { get; set; }

        /// <summary>
        /// If given then in each message to PubSub will be added as many attributes as elements has de dictionary, where
        /// the key corresponds to an attribute name and the value corresponds to its value to set.
        /// </summary>
        public Dictionary<string,string> MessageAttrFixed { get; set; }

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
        /// Configures the GoogleCloudPubSub sink default values.
        /// </summary>
        protected GoogleCloudPubSubSinkOptions()
        {
            //--- Constructor with not null/zero default values -----------------

            this.BatchPostingLimit = 50; 
            this.CustomFormatter = new GoogleCloudPubSubRawFormatter();     // Default formatter: raw data.
            //TODO: Temporally not used: this.ThrowPublishExceptions = true;
            this.BufferFileExtension = ".swap";
            this.BufferLogShippingInterval = TimeSpan.FromSeconds(2);
            this.Period = TimeSpan.FromSeconds(2);
            this.MessageDataToBase64 = true;
            this.BufferRollingSpecifier = "{Hour}";
            this.BufferRetainedFileCountLimit = 31;
            this.ErrorRetainedFileCountLimit = 31;
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

        /// <summary>
        /// Sets the specified values.
        /// </summary>
        /// <param name="bufferBaseFilename">Path to directory and file name prefix that can be used as a log shipping buffer for increasing the reliability of the log forwarding.</param>
        /// <param name="bufferFileSizeLimitBytes">The maximum size, in bytes, to which the buffer file for a specific date will be allowed to grow. 
        /// Once the limit is reached no more events will be stored. Pass null for default value.</param>
        /// <param name="bufferLogShippingIntervalMilisec">The interval, in miliseconds, between checking the buffer files. Pass null for default value.</param>
        /// <param name="bufferRetainedFileCountLimit">The maximum number of buffer files that will be retained, including the current buffer file. Pass null for default value (no limit). The minimum value is 2.</param>
        /// <param name="bufferFileExtension">The file extension to use with buffer files. Pass null for default value.</param>
        /// <param name="batchPostingLimit">The maximum number of events to post in a single batch. Pass null for default value.</param>
        /// <param name="batchSizeLimitBytes">The maximum size, in bytes, of the batch to send to PubSub. By default no limit will be applied.</param>
        /// <param name="minimumLogEventLevel">The minimum log event level required in order to write an event to the sink. Pass null for default value.</param>
        /// <param name="errorBaseFilename">Path to directory that can be used as a log shipping for storing internal errors.
        /// If set then it means we want to store errors. It can be used the same path as the buffer log (bufferBaseFilename) but the file name can't start with the same string.</param>
        /// <param name="errorFileSizeLimitBytes">The maximum size, in bytes, to which the error file for a specific date will be allowed to grow. By default no limit will be applied.</param>
        /// <param name="errorStoreEvents">If set to 'true' then events related to any error will be saved to the error file (after the error message). Pass null for default value (false).</param>
        /// <param name="debugStoreBatchLimitsOverflows">If set to 'true' then overflows when creating batch posts will be stored (overflows for BatchPostingLimit and also for BatchSizeLimitBytes). Pass null for default value (false).</param>
        /// <param name="debugStoreAll">If set to 'true' then debug data will be stored. Pass null for default value (false).</param>
        /// <param name="messageDataToBase64">If set to 'true' then data on PubSub messages is converted to Base64. Pass null for default value (true).</param>
        /// <param name="eventFieldSeparator">Fields separator in event data.</param>
        /// <param name="messageAttrMinValue">If given indicates that the PubSub message has to contain an attribute that is obtained as the MIN value for a concret field in the event dada.</param>
        /// <param name="messageAttrFixed">If given then in each message to PubSub will be added as many attributes as elements has de dictionary, where
        /// the key corresponds to an attribute name and the value corresponds to its value to set.</param>
        /// <param name="debugStoreEventSkip">If set to 'true' then skiped events (greater than the BatchSizeLimitBytes) will be stored.</param>
        /// <param name="bufferRollingSpecifier">Rolling specifier: {Date}, {Hour} or {HalfHour}. The default one is {Hour}.</param>
        /// <param name="errorRetainedFileCountLimit">The maximum number of error log files that will be retained, including the current error file. For unlimited retention, pass null. The default is 31.</param>
        /// <param name="debugStoreFileAction">If set to 'true' then all file actions (move forward, delete, ...) will me stored.</param>
        public void SetValues(
            string bufferBaseFilename,
            long? bufferFileSizeLimitBytes = null,
            int? bufferLogShippingIntervalMilisec = null,
            int? bufferRetainedFileCountLimit = null,
            string bufferFileExtension = null,
            int? batchPostingLimit = null,
            long? batchSizeLimitBytes = null,
            LogEventLevel minimumLogEventLevel = LevelAlias.Minimum,
            string errorBaseFilename = null,
            long? errorFileSizeLimitBytes = null,
            bool? errorStoreEvents = null,
            bool? debugStoreBatchLimitsOverflows = null,
            bool? debugStoreAll = null,
            bool? messageDataToBase64 = null,
            string eventFieldSeparator = null,
            string messageAttrMinValue = null,
            Dictionary<string, string> messageAttrFixed = null,
            bool? debugStoreEventSkip = null,
            string bufferRollingSpecifier = null,
            int? errorRetainedFileCountLimit = null,
            bool? debugStoreFileAction = null)
        {
            this.BufferBaseFilename = bufferBaseFilename;
            this.ErrorBaseFilename = errorBaseFilename;

            this.MinimumLogEventLevel = minimumLogEventLevel;

            //---

            if (bufferFileSizeLimitBytes != null)
                this.BufferFileSizeLimitBytes = bufferFileSizeLimitBytes.Value;

            if (bufferLogShippingIntervalMilisec != null)
                this.BufferLogShippingInterval = TimeSpan.FromMilliseconds(bufferLogShippingIntervalMilisec.Value);

            if (bufferRetainedFileCountLimit != null)
                this.BufferRetainedFileCountLimit = (bufferRetainedFileCountLimit.Value < 2 ? 2 : bufferRetainedFileCountLimit.Value);

            if (!string.IsNullOrEmpty(bufferFileExtension))
                this.BufferFileExtension = bufferFileExtension;

            if (!string.IsNullOrEmpty(bufferRollingSpecifier))
                this.BufferRollingSpecifier = bufferRollingSpecifier;

            //---

            if (batchPostingLimit != null)
                this.BatchPostingLimit = batchPostingLimit.Value;

            if (batchSizeLimitBytes != null)
                this.BatchSizeLimitBytes = batchSizeLimitBytes.Value;

            //---

            if (errorFileSizeLimitBytes != null)
                this.ErrorFileSizeLimitBytes = errorFileSizeLimitBytes.Value;

            if (errorStoreEvents != null)
                this.ErrorStoreEvents = errorStoreEvents.Value;

            if (debugStoreBatchLimitsOverflows != null)
                this.DebugStoreBatchLimitsOverflows = debugStoreBatchLimitsOverflows.Value;

            if (debugStoreAll != null)
                this.DebugStoreAll = debugStoreAll.Value;

            if (debugStoreEventSkip != null)
                this.DebugStoreEventSkip = debugStoreEventSkip.Value;

            if (errorRetainedFileCountLimit != null)
                this.ErrorRetainedFileCountLimit = errorRetainedFileCountLimit.Value;

            if (debugStoreFileAction != null)
                this.DebugStoreFileAction = debugStoreFileAction.Value;

            //---

            if (messageDataToBase64 != null)
                this.MessageDataToBase64 = messageDataToBase64.Value;

            if (!string.IsNullOrEmpty(eventFieldSeparator))
                this.EventFieldSeparator = eventFieldSeparator;

            if (!string.IsNullOrEmpty(messageAttrMinValue))
                this.MessageAttrMinValue = messageAttrMinValue;

            if (messageAttrFixed != null)
                this.MessageAttrFixed = messageAttrFixed;
            
        }


        #endregion
    }
}
