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
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Serilog.Sinks.PeriodicBatching;
using Serilog.Events;
using Serilog.Debugging;
using Google.Pubsub.V1;
using Google.Protobuf;

namespace Serilog.Sinks.GoogleCloudPubSub
{
   
    /// <summary>
    /// Writes log events as records to an Google Cloud Pub Sub.
    /// </summary>
    public class GoogleCloudPubSubSink : PeriodicBatchingSink
    {
        private readonly GoogleCloudPubSubSinkState _state;

        /// <summary>
        /// Construct a sink that saves logs to the specified Google PubSub account.
        /// </summary>
        /// <param name="options">Options configuring how the sink behaves, may NOT be null</param>
        public GoogleCloudPubSubSink(GoogleCloudPubSubSinkOptions options )
            : base(options.BatchSizeLimit, options.Period)
        {
            _state = GoogleCloudPubSubSinkState.Create(options);
        }

       /// <summary>
        /// Emit a batch of log events, running to completion synchronously.
        /// </summary>
        /// <param name="events">The events to emit.</param>
        /// <remarks>Override either <see cref="PeriodicBatchingSink.EmitBatch"/> or <see cref="PeriodicBatchingSink.EmitBatchAsync"/>,
        /// not both.</remarks>
        protected override async Task EmitBatchAsync(IEnumerable<LogEvent> events)
        {
            var payload = new List<PubsubMessage>();
           foreach (var logEvent in events){
                 StringWriter sw = new StringWriter();
                 this._state.PeriodicBatchingFormatter.Format( logEvent,sw );

                payload.Add(
                    new PubsubMessage{
                        // The data is any arbitrary ByteString. Here, we're using text.
                        Data = ByteString.CopyFromUtf8(sw.ToString())
                    }
                );
            }

           PublishResponse response = await  this._state.PublishAsync( payload );
        
           //TODO: Check response to log errors 
            /*
           var publishResponse = await _pubsubService.Projects.Topics.Publish( publishRequest, _topicPath ).ExecuteAsync().ConfigureAwait(false);
           if ( this._throwPublishExceptions ){
               if ( ( publishResponse.MessageIds == null) || ( publishResponse.MessageIds.Count != publishRequest.Messages.Count )){
                    throw new LoggingFailedException($"Received failed response. Messages requests {publishRequest.Messages.Count} responses {publishResponse.MessageIds.Count}");
               }
           }
           */
        
        }
         
    }

}