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
        /// <param name="bufferRetainedFileCountLimit">The maximum number of buffer files that will be retained, including the current buffer file. Pass null for default value.</param>
        /// <param name="bufferFileExtension">The file extension to use with buffer files. Pass null for default value.</param>
        /// <param name="batchSizeLimit">The maximum number of events to post in a single batch. Pass null for default value.</param>
        /// <returns>LoggerConfiguration object</returns>
        /// <exception cref="ArgumentNullException"><paramref name="projectId"/> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="topicId"/> is <see langword="null" />.</exception>
        /// /// <exception cref="ArgumentNullException"><paramref name="bufferBaseFilename"/> is <see langword="null" />.</exception>
        public static LoggerConfiguration GoogleCloudPubSub(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            string projectId,
            string topicId,
            string bufferBaseFilename,
            long? bufferFileSizeLimitBytes = null,
            int? bufferLogShippingIntervalMilisec = null,
            int? bufferRetainedFileCountLimit = null,
            string bufferFileExtension = null,
            int? batchSizeLimit = null)
        {

            //--- Creating an options object with the received parameters -------------
            // If a parameter is null then the corresponding option will not be set and it will be used its default value.

            GoogleCloudPubSubSinkOptions options = new GoogleCloudPubSubSinkOptions(projectId, topicId);
            options.BufferBaseFilename = bufferBaseFilename;

            if (bufferFileSizeLimitBytes != null)
                options.BufferFileSizeLimitBytes = bufferFileSizeLimitBytes.Value;

            if (bufferLogShippingIntervalMilisec != null)
                options.BufferLogShippingInterval = TimeSpan.FromMilliseconds(bufferLogShippingIntervalMilisec.Value);

            if (bufferRetainedFileCountLimit != null)
                options.BufferRetainedFileCountLimit = bufferRetainedFileCountLimit.Value;

            if (!string.IsNullOrEmpty(bufferFileExtension))
                options.BufferFileExtension = bufferFileExtension;

            if (batchSizeLimit != null)
                options.BatchSizeLimit = batchSizeLimit.Value;

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
        public static LoggerConfiguration GoogleCloudPubSub(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            GoogleCloudPubSubSinkOptions options = null)
        {

            // If options are not given then a default dummy options object is created 
            // (it may cause a fault as mandatory options have not been set).
            options = options ?? new GoogleCloudPubSubSinkOptions(null, null);

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
