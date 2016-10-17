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

using System.IO;
using Serilog.Formatting;
using Serilog.Events;

namespace Serilog.Sinks.GoogleCloudPubSub.Formatters
{
    /// <summary>
    /// This formatter send only the messageTemplate part of the event.
    /// </summary>
    public class GoogleCloudPubSubRawFormatter : ITextFormatter
    {

        //*******************************************************************
        //      ITextFormatter
        //*******************************************************************

        #region
        /// <summary>
        /// Format log events to GoogleCloudPubsub message format
        /// </summary>
        /// <param name="logEvent">Event to format</param>
        /// <param name="output">Output to write event</param>
        public void Format(LogEvent logEvent, TextWriter output)
        {
            // This method is executed each time a log event has to be stored (into memory, into the buffer file, ...)
            // In this development data is stored as raw data: no format is given.
            // Each log is stored in a separate line.

            output.WriteLine(logEvent.MessageTemplate.Text);
        }
        #endregion
    }

}