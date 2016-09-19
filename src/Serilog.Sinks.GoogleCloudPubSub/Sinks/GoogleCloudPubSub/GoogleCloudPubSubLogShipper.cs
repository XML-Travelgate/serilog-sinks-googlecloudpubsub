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
using System.Text;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Serilog.Debugging;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Serilog.Sinks.GoogleCloudPubSub
{
    class GoogleCloudPubSubLogShipper : IDisposable
    {

        //*******************************************************************
        //      PRIVATE FIELDS
        //*******************************************************************

        #region
        private readonly GoogleCloudPubSubSinkState _state;
        readonly int _batchPostingLimit;
        readonly int? _retainedFileCountLimit;
        readonly Timer _timer;
        readonly ExponentialBackoffConnectionSchedule _connectionSchedule;
        readonly string _bookmarkFilename;
        readonly string _logFolder;
        readonly string _candidateSearchPath;
        readonly object _stateLock = new object();
        volatile bool _unloading;
        #endregion



        //*******************************************************************
        //      CONSTRUCTOR
        //*******************************************************************

        #region
        internal GoogleCloudPubSubLogShipper(GoogleCloudPubSubSinkState state)
        {
            this._state = state;
            this._connectionSchedule = new ExponentialBackoffConnectionSchedule(this._state.Options.BufferLogShippingInterval.Value );
            this._batchPostingLimit = this._state.Options.BatchPostingLimit;
            this._retainedFileCountLimit = this._state.Options.BufferRetainedFileCountLimit;
            this._bookmarkFilename = Path.GetFullPath(this._state.Options.BufferBaseFilename + ".bookmark");
            this._logFolder = Path.GetDirectoryName(this._bookmarkFilename);
            this._candidateSearchPath = Path.GetFileName(this._state.Options.BufferBaseFilename) + "*" + this._state.Options.BufferFileExtension;

            this._timer = new Timer(s => OnTick());
            
            AppDomain.CurrentDomain.DomainUnload += OnAppDomainUnloading;
            AppDomain.CurrentDomain.ProcessExit += OnAppDomainUnloading;

            SetTimer();
        }
        #endregion


        //*******************************************************************
        //      
        //*******************************************************************

        #region
        void OnAppDomainUnloading(object sender, EventArgs args)
        {
            CloseAndFlush();
        }

        void CloseAndFlush()
        {
            lock (this._stateLock)
            {
                if (this._unloading)
                    return;

                this._unloading = true;
            }

            AppDomain.CurrentDomain.DomainUnload -= OnAppDomainUnloading;
            AppDomain.CurrentDomain.ProcessExit -= OnAppDomainUnloading;

            var wh = new ManualResetEvent(false);
            if (this._timer.Dispose(wh))
                wh.WaitOne();

            OnTick();
        }

        void SetTimer()
        {
            // Note, called under _stateLock
            var infiniteTimespan = Timeout.InfiniteTimeSpan;
            this._timer.Change(this._connectionSchedule.NextInterval, infiniteTimespan);
        }
        #endregion




        //*******************************************************************
        //      OnTick : MAIN FUNCTIONALITY (send data)
        //*******************************************************************

        #region
        void OnTick()
        {
             try
            {
                var count = 0;

                do
                {
                    // Locking the bookmark ensures that though there may be multiple instances of this
                    // class running, only one will ship logs at a time.
                    using (var bookmark = System.IO.File.Open(this._bookmarkFilename, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read))
                    {


                        //--- 1st step : identify the current buffer file and position to read from --------------------------------------------

                        long nextLineBeginsAtOffset;    // Current position to read in the current buffer file.
                        string currentFilePath;         // Current buffer file path with data to read and send.

                        // NOTE: 
                        //      Data is recovered from one buffer file each time onTick is executed.

                        // We read the bookmark to know from which file/position continue reading for the last processed file...
                        GoogleCloudPubSubLogShipper.TryReadBookmark(bookmark, out nextLineBeginsAtOffset, out currentFilePath);

                        // Candidate buffer files (in the working folder): all and ordered by name to have a sequence of treatment.
                        string[] fileSet = this.GetFileSet();

                        // We don't have a bookmark or it is not pointing to an existing file...
                        if (currentFilePath == null || !System.IO.File.Exists(currentFilePath))
                        {
                            // We get the first file of the set and at the zero position.
                            nextLineBeginsAtOffset = 0;
                            currentFilePath = fileSet.FirstOrDefault();
                        }

                        // Anyway, we don't have a buffer file to read from, so we exit without sending data neither updating the bookmark.
                        if (currentFilePath == null) continue;

                        //--- File date recovery and file extension validation ---
                        // file name pattern: whatever-xxx-xxx-{date}.{ext}, whatever-xxx-xxx-{date}_1.{ext}, etc.
                        // lastToken should be something like {date}.{ext} or {date}_1.{ext} now
                        string lastToken = currentFilePath.Split('-').Last();                       
                        if (!lastToken.ToLowerInvariant().EndsWith(this._state.Options.BufferFileExtension))
                        {
                            throw new FormatException(string.Format("The file name '{0}' does not seem to follow the right file pattern - it must be named [whatever]-{{Date}}[_n].{extension}", Path.GetFileName(currentFilePath)));
                        }
                        string dateString = lastToken.Substring(0, 8);
                        DateTime date = DateTime.ParseExact(dateString, "yyyyMMdd", CultureInfo.InvariantCulture);




                        //--- 2nd step : read current buffer file and position ------------------------------------------------------

                        List<string> payloadStr = new List<string>();
                        count = 0;

                        using (var current = System.IO.File.Open(currentFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            current.Position = nextLineBeginsAtOffset;

                            string nextLine;
                            while (count < this._batchPostingLimit && GoogleCloudPubSubLogShipper.TryReadLine(current, ref nextLineBeginsAtOffset, out nextLine))
                            {
                                payloadStr.Add(nextLine);
                                ++count;
                            }
                        }



                        //--- 3rd step : send data (if we have any) and update bookmark ------------------------------------------------------

                        // Do we have data to send?
                        if (count > 0)
                        {
                            // ...yes, we have data, so come on...

                            //--- Sending data to Google PubSub... ------------
                            GoogleCloudPubSubClientResponse response = this._state.Publish(payloadStr);
                            //-----------------------------------------------

                            if (response.Success)
                            {
                                //--- OK ---
                                GoogleCloudPubSubLogShipper.WriteBookmark(bookmark, nextLineBeginsAtOffset, currentFilePath);
                                this._connectionSchedule.MarkSuccess();
                            }
                            else
                            {
                                //--- ERROR ---
                                this._connectionSchedule.MarkFailure();
                                SelfLog.WriteLine("Received failed GoogleCloudPubSub shipping result : {0}", response.ErrorMessage);
                                break;
                            }
                           
                        }
                        else
                        {
                            // ...no, we don't have data, but may be there is another buffer file waiting with data to be sent...

                            // For whatever reason, there's nothing waiting to send. This means we should try connecting again at the
                            // regular interval, so mark the attempt as successful.
                            this._connectionSchedule.MarkSuccess();

                            // Only advance the bookmark if no other process has the
                            // current file locked, and its length is as we found it.   
                            if (fileSet.Length == 2 && fileSet.First() == currentFilePath && IsUnlockedAtLength(currentFilePath, nextLineBeginsAtOffset))
                            {
                                GoogleCloudPubSubLogShipper.WriteBookmark(bookmark, 0, fileSet[1]);
                            }

                            //if (fileSet.Length > 2)
                            //{
                            //    // Once there's a third file waiting to ship, we do our
                            //    // best to move on, though a lock on the current file
                            //    // will delay this.

                            //    System.IO.File.Delete(fileSet[0]);
                            //}
                        }

                        
                        //--- Retained File Count Limit --------------
                        // If necessary, one obsolete fiel is deleted each time.
                        // It is done event there is or not data to send: it is possible that our application is sending data at any time.
                        if (fileSet.Length > 2 && fileSet.Length > this._retainedFileCountLimit && fileSet.First() != currentFilePath)
                        {
                            System.IO.File.Delete(fileSet[0]);
                        }
                        //--------------------------------------------


                    }
                }
                while (count == this._batchPostingLimit);
            }
            catch (Exception ex)
            {
                SelfLog.WriteLine("Exception while emitting periodic batch from {0}: {1}", this, ex);
                this._connectionSchedule.MarkFailure();
            }
            finally
            {
                lock (this._stateLock)
                {
                    if (!this._unloading)
                    {
                        SetTimer();
                    }
                }
            }
        }
        #endregion




        //*******************************************************************
        //      FILES MANAGEMENT FUNCTIONS
        //*******************************************************************

        #region

        static bool IsUnlockedAtLength(string file, long maxLen)
        {
            try
            {
                using (var fileStream = System.IO.File.Open(file, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read))
                {
                    return fileStream.Length <= maxLen;
                }
            }
            catch (IOException ex)
            {
                var errorCode = Marshal.GetHRForException(ex) & ((1 << 16) - 1);
                if (errorCode != 32 && errorCode != 33)
                {
                    SelfLog.WriteLine("Unexpected I/O exception while testing locked status of {0}: {1}", file, ex);
                }
            }
            catch (Exception ex)
            {
                SelfLog.WriteLine("Unexpected exception while testing locked status of {0}: {1}", file, ex);
            }

            return false;
        }

        static void WriteBookmark(FileStream bookmark, long nextLineBeginsAtOffset, string currentFile)
        {
            using (var writer = new StreamWriter(bookmark))
            {
                writer.WriteLine("{0}:::{1}", nextLineBeginsAtOffset, currentFile);
            }
        }

        static bool TryReadLine(Stream current, ref long nextStart, out string nextLine)
        {
            //var includesBom = nextStart == 0;

            if (current.Length <= nextStart)
            {
                nextLine = null;
                return false;
            }

            current.Position = nextStart;

            // Important not to dispose this StreamReader as the stream must remain open (and we can't use the overload with 'leaveOpen' as it's not available in .NET4
            var reader = new StreamReader(current, Encoding.UTF8, false, 128);
            nextLine = reader.ReadLine();

            if (nextLine == null)
                return false;

            nextStart += Encoding.UTF8.GetByteCount(nextLine) + Encoding.UTF8.GetByteCount(Environment.NewLine);
            //if (includesBom)
            //    nextStart += 3;

            return true;
        }


      static void TryReadBookmark(Stream bookmark, out long nextLineBeginsAtOffset, out string currentFile)
        {
            nextLineBeginsAtOffset = 0;
            currentFile = null;

            if (bookmark.Length != 0)
            {
                string current;

                // Important not to dispose this StreamReader as the stream must remain open (and we can't use the overload with 'leaveOpen' as it's not available in .NET4
                var reader = new StreamReader(bookmark, Encoding.UTF8, false, 128);
                current = reader.ReadLine();

                if (current != null)
                {
                    bookmark.Position = 0;
                    var parts = current.Split(new[] { ":::" }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2)
                    {
                        nextLineBeginsAtOffset = long.Parse(parts[0]);
                        currentFile = parts[1];
                    }
                }

            }
        }

        string[] GetFileSet()
        {
            return Directory.GetFiles(this._logFolder, this._candidateSearchPath)
                .OrderBy(n => n)
                .ToArray();
        }
        #endregion



        //*******************************************************************
        //      IDisposable
        //*******************************************************************

        #region
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
        #endregion


    }

        



}