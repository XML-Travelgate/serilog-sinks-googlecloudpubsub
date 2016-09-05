// Copyright 2016 Serilog Contributors
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

using Google.Pubsub.V1;

namespace Serilog.Sinks.GoogleCloudPubSub
{
    internal class GoogleCloudPubSubClientResponse
    {

        //*******************************************************************
        //      PROPERTIES
        //*******************************************************************

        #region
        /// <summary>
        /// Indicates if the execution of the call has been success or not.
        /// </summary>
        internal bool Success { get; set; }

        /// <summary>
        /// Call response: in case it has been success.
        /// </summary>
        internal PublishResponse PublishResponse { get; set; }

        /// <summary>
        /// Error message: in case it has not been success.
        /// </summary>
        internal string ErrorMessage { get; set; }
        #endregion



        //*******************************************************************
        //      CONSTRUCTORS
        //*******************************************************************

        #region
        /// <summary>
        /// Void constructor.
        /// </summary>
        public GoogleCloudPubSubClientResponse()
        {
            this.Success = false;
            this.ErrorMessage = null;
            this.PublishResponse = null;
        }

        /// <summary>
        /// Success call constructor.
        /// </summary>
        /// <param name="publishResponse">Google PubSub response data.</param>
        public GoogleCloudPubSubClientResponse(PublishResponse publishResponse) : this()
        {
            this.Success = true;
            this.ErrorMessage = null;
            this.PublishResponse = publishResponse;
        }

        /// <summary>
        /// Not success call constructor.
        /// </summary>
        /// <param name="errorMessage">Error description.</param>
        public GoogleCloudPubSubClientResponse(string errorMessage) : this()
        {
            this.Success = false;
            this.ErrorMessage = errorMessage;
            this.PublishResponse = null;
        }
        #endregion

    }
}
