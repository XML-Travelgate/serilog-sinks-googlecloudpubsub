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
using Serilog.Sinks.PeriodicBatching;
using Google.Apis.Pubsub.v1;
using Google.Apis.Pubsub.v1.Data;
using Google.Apis.Services;

namespace Serilog.Sinks.GoogleCloudPubSub
{
   
    /// <summary>
    /// Writes log events as records to an Google Cloud Pub Sub.
    /// </summary>
    public class GoogleCloudPubSubSink : PeriodicBatchingSink
    {
       private readonly PubsubService _pubsub;

        /// <summary>
        /// Construct a sink that saves logs to the specified Google PubSub account.
        /// </summary>
        /// <param name="batchSizeLimit"></param>
        /// <param name="period"></param>
        
        public GoogleCloudPubSubSink(int batchSizeLimit, TimeSpan period)
            : base(batchSizeLimit, period)
        {
             if (batchSizeLimit < 1 )
                throw new ArgumentException("batchSizeLimit must be between > 1 for Google Cloud Pub Sub");
        
             _pubsub = new PubsubService();
        
        }
    }

}