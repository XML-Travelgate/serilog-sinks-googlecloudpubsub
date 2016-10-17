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
using System.Threading.Tasks;
using System.Collections.Generic;
using Serilog.Formatting;
using Google.Pubsub.V1;
using Google.Protobuf;
using Serilog.Sinks.RollingFile;
using System.Text;
using Serilog.Sinks.GoogleCloudPubSub.Formatters;

namespace Serilog.Sinks.GoogleCloudPubSub
{
    internal class GoogleCloudPubSubSinkState
    {


        //*******************************************************************
        //      STATIC MEMBERS
        //*******************************************************************

        #region
        public static GoogleCloudPubSubSinkState Create(GoogleCloudPubSubSinkOptions options, RollingFileSink errorsRollingFileSink)
        {
            if (options == null)
                throw new ArgumentNullException("options");
             else
                return new GoogleCloudPubSubSinkState(options, errorsRollingFileSink);  
        }
        #endregion




        //*******************************************************************
        //      PRIVATE FIELDS AND PUBLIC ACCESS (only get)
        //*******************************************************************

        #region

        /// <summary>
        /// Google.Pubsub.V1 client to access Google PubSub
        /// </summary>
        private readonly PublisherClient _client;

        /// <summary>
        /// Topic on Google PubSub
        /// </summary>
        private readonly string _topic;

        private readonly GoogleCloudPubSubSinkOptions _options;
        private readonly ITextFormatter _periodicBatchingFormatter;
        private readonly ITextFormatter _durableFormatter;

        // RollingFileSink instance to manage the error file.
        // It is created by the Durable Sink.
        private readonly RollingFileSink _errorsRollingFileSink;

        public ITextFormatter PeriodicBatchingFormatter { get { return this._periodicBatchingFormatter; } }
        public ITextFormatter DurableFormatter { get { return this._durableFormatter; } }
        public GoogleCloudPubSubSinkOptions Options { get { return this._options; } }

        private readonly bool _attrMinCreate = false;
        private readonly int _attrMinPosition = -1;
        private readonly string _attrMinName = string.Empty;
        #endregion



        //*******************************************************************
        //      CONSTRUCTOR
        //*******************************************************************

        #region
        private GoogleCloudPubSubSinkState(GoogleCloudPubSubSinkOptions options, RollingFileSink errorsRollingFileSink)
        {
            //--- Mandatory options validations --------------------
            if (options.BatchPostingLimit < 1 ) throw new ArgumentException("batchPostingLimit must be >= 1");
            if (string.IsNullOrWhiteSpace(options.ProjectId)) throw new ArgumentException("options.ProjectId");
            if (string.IsNullOrWhiteSpace(options.TopicId)) throw new ArgumentException("options.TopicId");

            //---
            // All is ok ...

            this._options = options;
            this._errorsRollingFileSink = errorsRollingFileSink;

            this._periodicBatchingFormatter = options.CustomFormatter ?? new GoogleCloudPubSubRawFormatter();
            this._durableFormatter = options.CustomFormatter ?? new GoogleCloudPubSubRawFormatter();

            this._topic = PublisherClient.FormatTopicName(options.ProjectId, options.TopicId);
            this._client = PublisherClient.Create();

            //---

            try
            {
                if (!string.IsNullOrWhiteSpace(this.Options.EventFieldSeparator) && !string.IsNullOrWhiteSpace(this.Options.MessageAttrMinValue))
                {
                    string[] auxArray = this.Options.MessageAttrMinValue.Split(new char[] { '#' }, StringSplitOptions.RemoveEmptyEntries);
                    if (auxArray.Length == 2)
                    {
                        this._attrMinPosition = Int32.Parse(auxArray[0]);
                        this._attrMinName = auxArray[1];
                        this._attrMinCreate = true;
                    }
                }
            }
            catch
            {
                this._attrMinCreate = false;
            }
        }
        #endregion




        //*******************************************************************
        //      PUBLISH AND AUXILIARY FUNCTIONS
        //*******************************************************************

        #region

        public async Task<GoogleCloudPubSubClientResponse> PublishAsync(IEnumerable<PubsubMessage> messages)
        {
            // This method sends data to Google PubSub using Google.Pubsub.V1.

            try
            {
                PublishResponse response = await this._client.PublishAsync(this._topic, messages);
                return new GoogleCloudPubSubClientResponse(response);
            }
            catch (Exception ex)
            {
                return new GoogleCloudPubSubClientResponse(ex.Message);
            }
        }

        public GoogleCloudPubSubClientResponse Publish(IEnumerable<PubsubMessage> messages)
        {
            // This method sends data to Google PubSub using Google.Pubsub.V1.

            try
            {
                PublishResponse response = this._client.Publish(this._topic, messages);
                return new GoogleCloudPubSubClientResponse(response);
            }
            catch (Exception ex)
            {
                return new GoogleCloudPubSubClientResponse(ex.Message);
            }
        }

        public GoogleCloudPubSubClientResponse Publish(IEnumerable<string> dataList)
        {
            // This method converts and sends data to Google PubSub: creates a unique message containing all the payload information.
            PubsubMessage message = this.ConvertToPubsubMessage(dataList);
            List<PubsubMessage> messages = new List<PubsubMessage>();
            messages.Add(message);
            return this.Publish(messages);
        }

        //---------

        public PubsubMessage ConvertToPubsubMessage(IEnumerable<string> dataList)
        {
            StringBuilder sb = new StringBuilder();
            string attrMinValue = null;

            if (dataList != null)
            {
                foreach (string data in dataList)
                {
                    sb.AppendLine(data);

                    //---

                    if (this._attrMinCreate)
                    {
                        attrMinValue = this._CalculateMinValue(attrMinValue, data);
                    }
                }
            }
             
            //---

            PubsubMessage message = new PubsubMessage();

            //---

            if (this._options.MessageDataToBase64)
            {
                message.Data = ByteString.FromBase64(Convert.ToBase64String(Encoding.UTF8.GetBytes(sb.ToString())));
            }
            else
            {
                message.Data = ByteString.CopyFromUtf8(sb.ToString());
            }

            if (this._attrMinCreate)
            {
                message.Attributes.Add(this._attrMinName, (attrMinValue ?? string.Empty));
            }

            if (this._options.MessageAttrFixed != null)
            {
                foreach (KeyValuePair<string,string> kv in this._options.MessageAttrFixed)
                {
                    message.Attributes.Add(kv.Key, (kv.Value ?? string.Empty));
                }
            }

            //---

            return message;
        }

        //public List<PubsubMessage> ConvertToPubsubMessageList(IEnumerable<string> dataList)
        //{
        //    List<PubsubMessage> messages = new List<PubsubMessage>();

        //    if (dataList != null)
        //    {
        //        foreach (string data in dataList)
        //        {
        //            messages.Add(
        //                new PubsubMessage
        //                {
        //                    // The data is any arbitrary ByteString. Here, we're using text.
        //                    Data = ByteString.CopyFromUtf8(data)
        //                }
        //            );
        //        }
        //    }

        //    return messages;
        //}

        private string _CalculateMinValue(string attrMinValue, string data)
        {
            try
            {
                string[] auxArray = data.Split(new string[] { this.Options.EventFieldSeparator }, StringSplitOptions.RemoveEmptyEntries);
                if (auxArray.Length > this._attrMinPosition)
                {
                    string currentDataValue = auxArray[this._attrMinPosition];
                    if (!string.IsNullOrWhiteSpace(currentDataValue) && (attrMinValue == null || currentDataValue.CompareTo(attrMinValue) < 0))
                    {
                        return currentDataValue;
                    }
                }
            }
            catch
            {    
            }

            return attrMinValue;
        }

        #endregion



        //*******************************************************************
        //      ERROR/DEBUG LOG AND AUXILIARY FUNCTIONS
        //*******************************************************************

        #region

        public void Error(string message)
        {
            this._ErrorDebugStore(message, null, false);
        }

        public void Error(string message, string simplePayloadStr)
        {
            List<string> payloadStr = new List<string>();
            payloadStr.Add(simplePayloadStr);
            this.Error(message, payloadStr);
        }

        public void Error(string message, List<string> payloadStr)
        {
            try
            {
                bool savePayload = ((this._options.ErrorStoreEvents || this._options.DebugStoreAll));
                this._ErrorDebugStore(message, payloadStr, savePayload);
            }
            catch (Exception ex)
            {
                //If any problem it will be ignored.
            }
        }

        //---

        public void Debug(string message)
        {
            if (this.Options.DebugStoreAll)
            {
                this._ErrorDebugStore(message, null, false);
            }
        }

        public void Debug(string message, List<string> payloadStr)
        {
            if (this.Options.DebugStoreAll)
            {
                this._ErrorDebugStore(message, payloadStr, true);
            }
        }

        //---

        public void DebugOverflow(string message, int count, int batchPostingLimit, long payloadSizeByte, long? batchSizeLimitByte)
        {
            try
            {
                if (this.Options.DebugStoreAll || this.Options.DebugStoreBatchLimitsOverflows)
                {
                    string overflowMessage = $"{message} Overflow. // Events in payload={count} with limit={batchPostingLimit} // Size (bytes) of payload={payloadSizeByte} with limit={(batchSizeLimitByte == null ? "no limit" : batchSizeLimitByte.Value.ToString())}";
                    this._ErrorDebugStore(overflowMessage, null, false);
                }
            }
            catch
            {
            }
        }

        //---

        public void DebugEventSkip(string message, string eventData)
        {
            try
            {
                if (this.Options.DebugStoreAll || this.Options.DebugStoreEventSkip)
                {
                    string skipMessage = $"{message} Event skipped. // {eventData ?? "-no data-"}";
                    this._ErrorDebugStore(skipMessage, null, false);
                }
            }
            catch
            {
            }
        }

        //---

        public void _ErrorDebugStore(string message, List<string> payloadStr, bool savePayload)
        {
            // This method stores an error or debug information (if necessary).
            try
            {
                if (this._errorsRollingFileSink != null && !string.IsNullOrEmpty(message))
                {
                    string timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " ";

                    this._errorsRollingFileSink.Emit(this._CreateErrorLogEvent(timeStamp + message));

                    //if (savePayload)
                    //{
                    //    if (payloadStr != null && payloadStr.Count > 0)
                    //    {
                    //        this._errorsRollingFileSink.Emit(this._CreateErrorLogEvent(" ---Events---"));
                    //        foreach (string str in payloadStr)
                    //        {
                    //            this._errorsRollingFileSink.Emit(this._CreateErrorLogEvent(str));
                    //        }
                    //        this._errorsRollingFileSink.Emit(this._CreateErrorLogEvent(" ----end-----"));
                    //    }
                    //    else
                    //    {
                    //        this._errorsRollingFileSink.Emit(this._CreateErrorLogEvent(" ---Events: there are no events.---"));
                    //    }
                    //}
                }
            }
            catch (Exception ex)
            {
                //If any problem it will be ignored.
            }
        }

        private Events.LogEvent _CreateErrorLogEvent(string message)
        {
            Events.MessageTemplate messTemplate = new Events.MessageTemplate(message, new List<Parsing.MessageTemplateToken>());
            Events.LogEvent logEvent = new Events.LogEvent(DateTimeOffset.Now, Events.LogEventLevel.Error, null, messTemplate, new List<Events.LogEventProperty>());
            return logEvent;
        }

        #endregion
    }

}