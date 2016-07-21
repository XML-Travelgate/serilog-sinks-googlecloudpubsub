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

namespace Serilog.Sinks.GoogleCloudPubSub
{
    internal class GoogleCloudPubSubSinkState
    {
        public static GoogleCloudPubSubSinkState Create( GoogleCloudPubSubSinkOptions options)
        {
            if (options == null)
                throw new ArgumentNullException("options");
             else
                return new GoogleCloudPubSubSinkState(options);  
        }
    
        private readonly GoogleCloudPubSubSinkOptions _options;
        private readonly PublisherClient _client;
        private readonly string _topic;
        private readonly ITextFormatter _periodicBatchingFormatter;
        private readonly ITextFormatter _durableFormatter;
        public ITextFormatter PeriodicBatchingFormatter { get { return this._periodicBatchingFormatter; } }
        public ITextFormatter DurableFormatter { get { return this._durableFormatter; } }
        public GoogleCloudPubSubSinkOptions Options { get { return this._options; } }
       
        private GoogleCloudPubSubSinkState(GoogleCloudPubSubSinkOptions options)
        {
            if (options.BatchSizeLimit < 1 ) throw new ArgumentException("batchSizeLimit must be >= 1");
            if (string.IsNullOrWhiteSpace(options.ProjectId)) throw new ArgumentException("options.ProjectId");
            if (string.IsNullOrWhiteSpace(options.TopicId)) throw new ArgumentException("options.TopicId");
         
            _options = options;

            _periodicBatchingFormatter = options.CustomFormatter ?? new GoogleCloudPubSubRawFormatter();
            _durableFormatter = options.CustomFormatter ?? new GoogleCloudPubSubRawFormatter();

            _topic = PublisherClient.FormatTopicName(options.ProjectId, options.TopicId);
            _client = PublisherClient.Create();
        }

        public async Task<PublishResponse> PublishAsync(IEnumerable<PubsubMessage> messages){
                //TODO: Configure CallSettings
                return await this._client.PublishAsync(this._topic, messages);
        }

    }

}