// Serilog.Sinks.Seq Copyright 2016 Serilog Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.IO;
using System.Threading;

namespace Serilog.Sinks.GoogleCloudPubSub
{
    class GoogleCloudPubSubLogShipper : IDisposable
    {
        private readonly GoogleCloudPubSubSinkState _state;
        readonly int _batchSizeLimit;
        readonly Timer _timer;
        readonly ExponentialBackoffConnectionSchedule _connectionSchedule;
        readonly string _bookmarkFilename;
        readonly string _logFolder;
        readonly string _candidateSearchPath;
        readonly object _stateLock = new object();
        volatile bool _unloading;

        internal GoogleCloudPubSubLogShipper(GoogleCloudPubSubSinkState state)
        {
            _state = state;
            _connectionSchedule = new ExponentialBackoffConnectionSchedule( _state.Options.BufferLogShippingInterval.Value );    
            _batchSizeLimit = _state.Options.BatchSizeLimit;
            _bookmarkFilename = Path.GetFullPath(_state.Options.BufferBaseFilename + ".bookmark");
            _logFolder = Path.GetDirectoryName(_bookmarkFilename);
            _candidateSearchPath = Path.GetFileName(_state.Options.BufferBaseFilename) + "*.json";

             _timer = new Timer(s => OnTick());
            
            AppDomain.CurrentDomain.DomainUnload += OnAppDomainUnloading;
            AppDomain.CurrentDomain.ProcessExit += OnAppDomainUnloading;

            SetTimer();
        }    

        void OnAppDomainUnloading(object sender, EventArgs args)
        {
            CloseAndFlush();
        }

        void CloseAndFlush()
        {
            lock (_stateLock)
            {
                if (_unloading)
                    return;

                _unloading = true;
            }

            AppDomain.CurrentDomain.DomainUnload -= OnAppDomainUnloading;
            AppDomain.CurrentDomain.ProcessExit -= OnAppDomainUnloading;

            var wh = new ManualResetEvent(false);
            if (_timer.Dispose(wh))
                wh.WaitOne();

            OnTick();
        }
    
        /// <summary>
        /// /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            Dispose(true);
        }

         /// <summary>
        /// Free resources held by the sink.
        /// </summary>
        /// <param name="disposing">If true, called because the object is being disposed; if false,
        /// the object is being disposed from the finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;
            CloseAndFlush();
        }

        void SetTimer()
        {
            // Note, called under _stateLock
            var infiniteTimespan = Timeout.InfiniteTimeSpan;
            _timer.Change(_connectionSchedule.NextInterval, infiniteTimespan);
        }

        void OnTick()
        {
            
        }
    
    }

        

}