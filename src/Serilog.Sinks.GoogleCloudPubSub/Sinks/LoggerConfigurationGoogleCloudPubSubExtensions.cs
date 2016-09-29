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
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.GoogleCloudPubSub;
using System.Collections.Generic;

namespace Serilog
{


    /// <summary>
    /// Adds the WriteTo.GoogleCloudPubSub() extension method to <see cref="LoggerConfiguration"/>.
    /// </summary>
    public static class LoggerConfigurationGoogleCloudPubSubExtensions
    {


        //*******************************************************************
        //      EXTENSION CREATION
        //*******************************************************************

        #region

        /// <summary>
        /// Overload to allow configuration through AppSettings: creates a durable sink.
        /// </summary>
        /// <param name="loggerSinkConfiguration">Options for the sink.</param>
        /// <param name="projectId">Google Cloud PubSub Project ID</param>
        /// <param name="topicId">Google Cloud PubSub Topic ID</param>
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
        /// <param name="eventFieldSeparator">Fields seperator in event data.</param>
        /// <param name="messageAttrMinValue">If given indicates that the PubSub message has to contain an attribute that is obtained as the MIN value for a concret field in the event dada.</param>
        /// <param name="bufferWriteIsBuffered">If set to 'true' then the underlying stream will buffer writes to improve write performance.
        /// If set to 'false' (default value) each event write will be flushed to disk individually at that moment. Pass null for default value (false).
        /// IMPORTANT: activating the buffer doesn't guarantee events writing integrity. An event can be writen to disk not with its
        /// full information (because the buffer is full and it has not space enought for all the event data) and then can be sent to PubSub in different messages.</param>
        /// <param name="messageAttrFixed">If given then in each message to PubSub will be added as many attributes as elements has de dictionary, where
        /// the key corresponds to an attribute name and the value corresponds to its value to set.</param>
        /// <param name="debugStoreEventSkip">If set to 'true' then skiped events (greater than the BatchSizeLimitBytes) will be stored.</param>
        /// <returns>LoggerConfiguration object</returns>
        /// <exception cref="ArgumentNullException"><paramref name="projectId"/> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="topicId"/> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="bufferBaseFilename"/> is <see langword="null" />.</exception>
        public static LoggerConfiguration GoogleCloudPubSub(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            string projectId,
            string topicId,
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
            bool? bufferWriteIsBuffered = null,
            Dictionary<string, string> messageAttrFixed = null,
            bool? debugStoreEventSkip = null)
        {

            //--- Creating an options object with the received parameters -------------
            // If a parameter is null then the corresponding option will not be set and it will be used its default value.

            GoogleCloudPubSubSinkOptions options = new GoogleCloudPubSubSinkOptions(projectId, topicId);
            options.SetValues(
                bufferBaseFilename,
                bufferFileSizeLimitBytes,
                bufferLogShippingIntervalMilisec,
                bufferRetainedFileCountLimit,
                bufferFileExtension,
                batchPostingLimit,
                batchSizeLimitBytes,
                minimumLogEventLevel,
                errorBaseFilename,
                errorFileSizeLimitBytes,
                errorStoreEvents,
                debugStoreBatchLimitsOverflows,
                debugStoreAll,
                messageDataToBase64,
                eventFieldSeparator,
                messageAttrMinValue,
                bufferWriteIsBuffered,
                messageAttrFixed,
                debugStoreEventSkip);


            //--- Mandatory parameters ------------
            ValidateMandatoryOptions(options);
            //---
            // All is ok ...

            return GoogleCloudPubSub(loggerSinkConfiguration, options);
        }


        //--------------------------------------


        /// <summary>
        /// Overload to use an specific configuration.
        /// 
        /// By passing in the options object the BufferBaseFilename, you make this into a durable sink. 
        /// Meaning it will log to disk first and tries to deliver to the PubSub server in the background.
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <param name="loggerSinkConfiguration">Options for the sink.</param>
        /// <param name="options">Provides options specific to the GoogleCloudPubSub sink</param>
        /// <returns>LoggerConfiguration object</returns>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null" />.</exception>
        public static LoggerConfiguration GoogleCloudPubSub(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            GoogleCloudPubSubSinkOptions options)
        {

            //--- Mandatory parameters ------------
            ValidateMandatoryOptions(options);
            //---
            // All is ok ...

            // -If we are given a BufferBaseFilename that means that log events have to be stored
            //  on a buffer file on disk and then sent to PubSub in groups deppending on a timer (in the background).
            //      In this case the used sink is : DurableGoogleCloudPubSubSink
            //
            // -If we are not given a BufferBaseFilename that means that log events have to be stored
            //  in memory.
            //      In this case the used sink is : GoogleCloudPubSubSink
            //
            // The choosen sink will contain the execution options.
            //
            var sink = string.IsNullOrWhiteSpace(options.BufferBaseFilename)
                ? (ILogEventSink)new GoogleCloudPubSubSink(options)
                : new DurableGoogleCloudPubSubSink(options);

            return loggerSinkConfiguration.Sink(sink, options.MinimumLogEventLevel ?? LevelAlias.Minimum);
        }

        //--------------------------------------


        /// <summary>
        /// Overload to use a given instance of the durable sink created and initialized outside.
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <param name="loggerSinkConfiguration">Options for the sink.</param>
        /// <param name="durableSink">Instance of the durable sink to use, configured outside.</param>
        /// <returns>LoggerConfiguration object</returns>
        public static LoggerConfiguration GoogleCloudPubSub(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            DurableGoogleCloudPubSubSink durableSink)
        {
            return loggerSinkConfiguration.Sink(durableSink, durableSink.MinimumLogEventLevel ?? LevelAlias.Minimum);
        }


        #endregion








        //*******************************************************************
        //      AUXILIARY FUNCTIONS
        //*******************************************************************

        #region
        private static void ValidateMandatoryOptions(GoogleCloudPubSubSinkOptions options)
        {
            //--- Mandatory data for any type of sink used (durable or not). -------------------
            if (string.IsNullOrEmpty(options.ProjectId))
                throw new ArgumentNullException(nameof(options.ProjectId), "No project specified.");
            if (string.IsNullOrEmpty(options.TopicId))
                throw new ArgumentNullException(nameof(options.TopicId), "No topic specified.");

            //--------------------------------------------------------------------
            // At this version we just support a durable sink !!!!
            if (string.IsNullOrEmpty(options.BufferBaseFilename))
                throw new ArgumentNullException(nameof(options.BufferBaseFilename), "This version just supports a durable sink. No buffer specified.");
            //--------------------------------------------------------------------

        }

        #endregion
    }
}
