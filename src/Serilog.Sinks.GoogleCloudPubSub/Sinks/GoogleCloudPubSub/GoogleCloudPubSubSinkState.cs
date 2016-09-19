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

namespace Serilog.Sinks.GoogleCloudPubSub
{
    internal class GoogleCloudPubSubSinkState
    {


        //*******************************************************************
        //      STATIC MEMBERS
        //*******************************************************************

        #region
        public static GoogleCloudPubSubSinkState Create(GoogleCloudPubSubSinkOptions options)
        {
            if (options == null)
                throw new ArgumentNullException("options");
             else
                return new GoogleCloudPubSubSinkState(options);  
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

        public ITextFormatter PeriodicBatchingFormatter { get { return this._periodicBatchingFormatter; } }
        public ITextFormatter DurableFormatter { get { return this._durableFormatter; } }
        public GoogleCloudPubSubSinkOptions Options { get { return this._options; } }
        #endregion



        //*******************************************************************
        //      CONSTRUCTOR
        //*******************************************************************

        #region
        private GoogleCloudPubSubSinkState(GoogleCloudPubSubSinkOptions options)
        {
            //--- Mandatory options validations --------------------
            if (options.BatchPostingLimit < 1 ) throw new ArgumentException("batchPostingLimit must be >= 1");
            if (string.IsNullOrWhiteSpace(options.ProjectId)) throw new ArgumentException("options.ProjectId");
            if (string.IsNullOrWhiteSpace(options.TopicId)) throw new ArgumentException("options.TopicId");

            //---
            // All is ok ...

            this._options = options;

            this._periodicBatchingFormatter = options.CustomFormatter ?? new GoogleCloudPubSubRawFormatter();
            this._durableFormatter = options.CustomFormatter ?? new GoogleCloudPubSubRawFormatter();

            this._topic = PublisherClient.FormatTopicName(options.ProjectId, options.TopicId);
            this._client = PublisherClient.Create();
        }
        #endregion




        //*******************************************************************
        //      PUBLISH AND AUXILIARY FUNCTIONS
        //*******************************************************************

        #region

        public async Task<GoogleCloudPubSubClientResponse> PublishAsync(IEnumerable<PubsubMessage> messages)
        {
            // This method sends data to Google PubSub using Google.Pubsub.V1.

            //return await this._client.PublishAsync(this._topic, messages);
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

        public GoogleCloudPubSubClientResponse Publish(IEnumerable<string> messagesStr)
        {
            // This method converts and sends data to Google PubSub.

            List<PubsubMessage> payload = this.ConvertToPubsubMessageList(messagesStr);
            return this.Publish(payload);
        }

        //---------

        public List<PubsubMessage> ConvertToPubsubMessageList (IEnumerable<string> dataList)
        {
            List<PubsubMessage> payload = new List<PubsubMessage>();

            if (dataList != null)
            {
                foreach (string data in dataList)
                {
                    payload.Add(
                        new PubsubMessage
                        {
                            // The data is any arbitrary ByteString. Here, we're using text.
                            Data = ByteString.CopyFromUtf8(data)
                        }
                    );
                }
            }

            return payload;
        }
        
        #endregion

    }

}