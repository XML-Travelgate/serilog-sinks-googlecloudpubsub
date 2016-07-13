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
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using Serilog.Sinks.PeriodicBatching;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Debugging;

using Google.Apis.Pubsub.v1;
using Google.Apis.Pubsub.v1.Data;



namespace Serilog.Sinks.GoogleCloudPubSub
{
   
    /// <summary>
    /// Writes log events as records to an Google Cloud Pub Sub.
    /// </summary>
    public class GoogleCloudPubSubSink : PeriodicBatchingSink
    {
       private readonly PubsubService _pubsubService;
       private readonly string _topicPath;
       private readonly ITextFormatter _formatter;
       private readonly bool _throwPublishExceptions;


        /// <summary>
        /// Construct a sink that saves logs to the specified Google PubSub account.
        /// </summary>
        /// <param name="options">Options configuring how the sink behaves, may NOT be null</param>
        public GoogleCloudPubSubSink(GoogleCloudPubSubSinkOptions options )
            : base(options.BatchSizeLimit, options.Period)
        {
            if (options.BatchSizeLimit < 1 ) throw new ArgumentException("batchSizeLimit must be between > 1 for Google Cloud Pub Sub");
            if (options.PubsubService == null) throw new ArgumentNullException("Pubsubservice is null");
         
             _pubsubService = options.PubsubService;
             _topicPath = options.TopicPath;
             _throwPublishExceptions = options.ThrowPublishExceptions;

             _formatter = options.CustomFormatter ?? new GoogleCloudPubSubRawFormatter();
        }

       /// <summary>
        /// Emit a batch of log events, running to completion synchronously.
        /// </summary>
        /// <param name="events">The events to emit.</param>
        /// <remarks>Override either <see cref="PeriodicBatchingSink.EmitBatch"/> or <see cref="PeriodicBatchingSink.EmitBatchAsync"/>,
        /// not both.</remarks>
        protected override async Task EmitBatchAsync(IEnumerable<LogEvent> events)
        {
           PublishRequest publishRequest = new PublishRequest();
           foreach (var logEvent in events){
                 var sw = new StringWriter();
                 this._formatter.Format( logEvent,sw ); 
                 publishRequest.Messages.Add( new PubsubMessage(){ Data = sw.ToString() } );
            }
           var publishResponse = await _pubsubService.Projects.Topics.Publish( publishRequest, _topicPath ).ExecuteAsync().ConfigureAwait(false);
           if ( this._throwPublishExceptions ){
               if ( ( publishResponse.MessageIds == null) || ( publishResponse.MessageIds.Count != publishRequest.Messages.Count )){
                    throw new LoggingFailedException($"Received failed response. Messages requests {publishRequest.Messages.Count} responses {publishResponse.MessageIds.Count}");
               }
           }
        }

    }

}