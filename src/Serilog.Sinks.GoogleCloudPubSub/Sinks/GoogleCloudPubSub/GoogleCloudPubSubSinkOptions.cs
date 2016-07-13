
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
using Google.Apis.Pubsub.v1;
using Serilog.Formatting;

namespace Serilog.Sinks.GoogleCloudPubSub
{
    /// <summary>
    /// Provides GoogleCloudPubSubSink with configurable options
    /// </summary>
    public class GoogleCloudPubSubSinkOptions
    {

        ///<summary>
        /// GoogleCloudOubSub service client.
        /// </summary>
        public PubsubService PubsubService{get;set;}

        ///<summary>
        /// GoogleCloudOubSub topic to publish.
        /// </summary>
        public string TopicPath{get;set;}

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

        ///<summary>
        /// Supplies culture-specific formatting information, or null.
        /// </summary>
       public  IFormatProvider FormatProvider {get;set;}


        /// <summary>
        ///  Customizes the formatter used when converting log events into ElasticSearch documents. Please note that the formatter output must be valid JSON :)
        /// </summary>
        public ITextFormatter CustomFormatter { get; set; }

        /// <summary>
        /// Configures the elasticsearch sink defaults
        /// </summary>
        protected GoogleCloudPubSubSinkOptions()
        {
            this.Period = TimeSpan.FromSeconds(1);
            this.BatchSizeLimit = 50;
            this.CustomFormatter = new GoogleCloudPubSubRawFormatter();
            this.ThrowPublishExceptions = true;
        }


      /// <summary>
        /// Configures the elasticsearch sink
        /// </summary>
        /// <param name="pubSubService">The pubSubService to use to write events to</param>
        /// <param name="topicPath">The pubSub topic to use to write events to</param>
        public GoogleCloudPubSubSinkOptions( PubsubService pubSubService, string topicPath)
            : this()
        {
            PubsubService = pubSubService;
            TopicPath = topicPath;
        }
    }
}
