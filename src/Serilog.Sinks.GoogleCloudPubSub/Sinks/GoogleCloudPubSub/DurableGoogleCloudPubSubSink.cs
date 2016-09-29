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
using System.Collections.Generic;

namespace Serilog.Sinks.GoogleCloudPubSub
{
    // This class uses a file on disk as a buffer previous sending data (in blocks) to the remote server.
    // It is used RollingFileSink to manage this buffer file.
    // the buffer file is coded with UTF-8 and without BOM.

    public class DurableGoogleCloudPubSubSink : ILogEventSink, IDisposable
    {

        //*******************************************************************
        //      PRIVATE FIELDS
        //*******************************************************************

        #region
        // Sufix for the file used with rolling file sink.
        // The main name and the extension are set in the options.
        const string FileNameSuffix = "-{Date}";

        // Google Cloud PubSub Shipper and State instances.
        private GoogleCloudPubSubSinkState _state;     // -> Contains the options.
        private GoogleCloudPubSubLogShipper _shipper;  // -> Contains the State and extracts and uses some options.

        // RollingFileSink instance to manage the buffer file.
        private RollingFileSink _dataRollingFileSink;

        // RollingFileSink instance to manage the error file.
        private RollingFileSink _errorsRollingFileSink;
        #endregion






        //*******************************************************************
        //      CONSTRUCTORS
        //*******************************************************************

        #region
        /// <summary>
        /// Default constructor with a given options object.
        /// </summary>
        /// <param name="options"></param>
        public DurableGoogleCloudPubSubSink(GoogleCloudPubSubSinkOptions options)
        {
            this.Initialize(options);
        }



        /// <summary>
        /// Constructor specifying concret options values.
        /// </summary>
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
        /// <param name="minimumLogEventLevel">The minimum log event level required in order to write an event to the sink. Pass null for default value (minimum).</param>
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
        public DurableGoogleCloudPubSubSink(
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

            //-----

            this.Initialize(options);
        }


        //--------------

        private void Initialize(GoogleCloudPubSubSinkOptions options)
        {
            //--- Mandatory options validations --------------------
            this.ValidateMandatoryOptions(options);

            //---
            // All is ok ... instances are created using the defined options...


            //--- RollingFileSink to store internal errors ------------------
            // It will be generated a file for each day.
            if (!string.IsNullOrWhiteSpace(options.ErrorBaseFilename))
            {
                this._errorsRollingFileSink = new RollingFileSink(
                        options.ErrorBaseFilename + FileNameSuffix + ".log",
                        new GoogleCloudPubSubRawFormatter(),   // Formatter for error info (raw).
                        options.ErrorFileSizeLimitBytes,
                        null
                    );
            }

            //---

            this._state = GoogleCloudPubSubSinkState.Create(options, this._errorsRollingFileSink);
            this._shipper = new GoogleCloudPubSubLogShipper(this._state);

            //---

            //--- RollingFileSink to store data to be sent to PubSub ------------------
            // It will be generated a file for each day.
            this._dataRollingFileSink = new RollingFileSink(
                    options.BufferBaseFilename + FileNameSuffix + options.BufferFileExtension,
                    this._state.DurableFormatter,   // Formatter for data to insert into the buffer file.
                    options.BufferFileSizeLimitBytes,
                    null,
                    encoding: System.Text.Encoding.UTF8,
                    buffered: options.BufferWriteIsBuffered
                );
            // NOTE: if the encoding is set to UTF8 then the BOM is inserted into the file and has to be
            //       taken in mind when reading from the file.
        }


        //--------------

        private void ValidateMandatoryOptions(GoogleCloudPubSubSinkOptions options)
        {
            if (string.IsNullOrEmpty(options.ProjectId))
            {
                throw new ArgumentNullException(nameof(options.ProjectId), "No project specified.");
            }

            if (string.IsNullOrEmpty(options.TopicId))
            {
                throw new ArgumentNullException(nameof(options.TopicId), "No topic specified.");
            }

            if (string.IsNullOrWhiteSpace(options.BufferBaseFilename))
            {
                throw new ArgumentException("Cannot create the durable GoogleCloudPubSub sink without BufferBaseFilename");
            }

            if (string.IsNullOrWhiteSpace(options.BufferFileExtension))
            {
                throw new ArgumentException("Cannot create the durable GoogleCloudPubSub sink without BufferFileExtension");
            }

            if (!options.BufferLogShippingInterval.HasValue)
            {
                throw new ArgumentException("Cannot create the durable GoogleCloudPubSub sink without BufferLogShippingInterval");
            }

            if (options.BufferRetainedFileCountLimit.HasValue && options.BufferRetainedFileCountLimit.Value < 2)
            {
                throw new ArgumentException("BufferRetainedFileCountLimit minimum value is 2");
            }
        }

        #endregion





        //*******************************************************************
        //      PUBLIC
        //*******************************************************************

        #region
        /// <summary>
        /// Returns de event level set in the options.
        /// </summary>
        public LogEventLevel? MinimumLogEventLevel
        {
            get
            {
                return this._state.Options.MinimumLogEventLevel;
            }
        }


        #endregion




        //*******************************************************************
        //      ILogEventSink
        //*******************************************************************

        #region
        public void Emit(LogEvent logEvent)
        {
            // This method is executed each time we write anything on the Serilog instance with this sink and its event
            // level is greater than or equal to the one set to the sink.
            // Log event is formatted using the assigned formatter (by default it is GoogleCloudPubSubRawFormatter)
            // and then the result string is stored into the buffer file that is managed by the RollingFileSink.
            this._dataRollingFileSink.Emit(logEvent);
        }
        #endregion



        //*******************************************************************
        //      IDisposable
        //*******************************************************************

        #region
        public void Dispose()
        {
            if (this._dataRollingFileSink != null)
            {
                this._dataRollingFileSink.Dispose();
            }

            if (this._errorsRollingFileSink != null)
            {
                this._errorsRollingFileSink.Dispose();
            }

            if (this._shipper != null)
            {
                this._shipper.Dispose();
            }
        }
        #endregion

    }

}
