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
using System.IO;
using System.Linq;
using System.Threading;
using Serilog.Debugging;
using System.Collections.Generic;
using System.Runtime.InteropServices;
#if DOTNETCORE
using System.Runtime.Loader;
#endif
#if NO_TIMER
using Serilog.Sinks.GoogleCloudPubSub.CrossPlatform;
#endif

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
        readonly long? _batchSizeLimitBytes;
        readonly int? _retainedFileCountLimit;
        readonly ExponentialBackoffConnectionSchedule _connectionSchedule;
        readonly string _bookmarkFilename;
        readonly string _logFolder;
        readonly string _candidateSearchPath;
        readonly object _stateLock = new object();
        volatile bool _unloading;

#if NO_TIMER
        readonly PortableTimer _timer;
#else
        readonly Timer _timer;
#endif

        private static string CNST_Shipper_Error = "Shipper [Error]: ";
        private static string CNST_Shipper_Debug = "Shipper [Debug]: ";

        private static readonly int CNST_NewLineBytes = Encoding.UTF8.GetByteCount(Environment.NewLine);

        // Stats for current file (to store when it is finished).
        private int linesSentOKForCurrentFile = 0;
        private int linesSentERRORForCurrentFile = 0;
        private int linesDroppedForCurrentFile = 0;
        private int overflowsForCurrentFile = 0;
        private int batchesSentOKForCurrentFile = 0;
        private int batchesSentERRORForCurrentFile = 0;
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
            this._batchSizeLimitBytes = this._state.Options.BatchSizeLimitBytes;
            this._retainedFileCountLimit = this._state.Options.BufferRetainedFileCountLimit;
            this._bookmarkFilename = Path.GetFullPath(this._state.Options.BufferBaseFilename + ".bookmark");
            this._logFolder = Path.GetDirectoryName(this._bookmarkFilename);
            this._candidateSearchPath = Path.GetFileName(this._state.Options.BufferBaseFilename) + "*" + this._state.Options.BufferFileExtension;

#if NO_TIMER
            this._timer = new PortableTimer(cancel => OnTick());
#else
            this._timer = new Timer(s => OnTick(), null, -1, -1);
#endif

#if DOTNETCORE
            System.Runtime.Loader.AssemblyLoadContext.Default.Unloading += OnAppDomainUnloading;
#else
            AppDomain.CurrentDomain.DomainUnload += OnAppDomainUnloading;
            AppDomain.CurrentDomain.ProcessExit += OnAppDomainUnloading;    
#endif

            SetTimer();
        }
        #endregion


        //*******************************************************************
        //      
        //*******************************************************************

        #region

#if DOTNETCORE
        void OnAppDomainUnloading(AssemblyLoadContext assContext)
        {
            CloseAndFlush();
        }
#else
        void OnAppDomainUnloading(object sender, EventArgs args)
        {
            CloseAndFlush();
        }  
#endif

        void CloseAndFlush()
        {
            lock (this._stateLock)
            {
                if (this._unloading)
                    return;

                this._unloading = true;
            }

#if DOTNETCORE
            System.Runtime.Loader.AssemblyLoadContext.Default.Unloading -= OnAppDomainUnloading;
#else
            AppDomain.CurrentDomain.DomainUnload -= OnAppDomainUnloading;
            AppDomain.CurrentDomain.ProcessExit -= OnAppDomainUnloading;    
#endif

#if NO_TIMER
            this._timer.Dispose();
#else
            var wh = new ManualResetEvent(false);
            if (this._timer.Dispose(wh))
                wh.WaitOne();
#endif

            OnTick();
        }

        void SetTimer()
        {
            // Note, called under _stateLock
            var infiniteTimespan = Timeout.InfiniteTimeSpan;
#if NO_TIMER
            this._timer.Start(_connectionSchedule.NextInterval);
#else
            this._timer.Change(this._connectionSchedule.NextInterval, infiniteTimespan);
#endif
        }
        #endregion




        //*******************************************************************
        //      OnTick : MAIN FUNCTIONALITY (send data)
        //*******************************************************************

        #region
        void OnTick()
        {
            string currentFilePath = null;

            try
            {
                bool continueWhile = false;

                do
                {
                    // Locking the bookmark ensures that though there may be multiple instances of this
                    // class running, only one will ship logs at a time.
                    using (FileStream bookmark = System.IO.File.Open(this._bookmarkFilename, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read))
                    {

                        //--- 1st step : identify the current buffer file and position to read from --------------------------------------------

                        long nextLineBeginsAtOffset;    // Current position to read in the current buffer file.
                        currentFilePath = null;         // Current buffer file path with data to read and send.

                        // NOTE: 
                        //      Data is recovered from one buffer file each time onTick is executed.

                        // We read the bookmark to know from which file/position continue reading for the last processed file...
                        GoogleCloudPubSubLogShipper.TryReadBookmark(bookmark, out nextLineBeginsAtOffset, out currentFilePath);

                        // Candidate buffer files (in the working folder): all and ordered by name to have a sequence of treatment.
                        string[] fileSet = GoogleCloudPubSubLogShipper.GetFileSet(this._logFolder, this._candidateSearchPath);

                        // We don't have a bookmark or it is not pointing to an existing file...
                        if (currentFilePath == null || !System.IO.File.Exists(currentFilePath))
                        {
                            // We get the first file of the set and at the zero position.
                            nextLineBeginsAtOffset = 0;
                            currentFilePath = fileSet.FirstOrDefault();
                        }

                        // Anyway, we don't have a buffer file to read from, so we exit without sending data neither updating the bookmark.
                        if (currentFilePath == null) continue;  // >>>>>>>>>>>>>

                        // Position of the current file in the set.
                        int currentFileSetPosition = 0;
                        if (!GoogleCloudPubSubLogShipper.GetFileSetPosition(fileSet, currentFilePath, out currentFileSetPosition))
                        {
                            this._state.Error($"The current file (bookmark) does not exist in the file set and I don't know what to do. [{currentFilePath}]");
                            continue;  // >>>>>>>>>>>>>
                        }

                        // It is not done any file date recovery or file extension validation to do not disturb the main app.
                        // All files that match the template (that are contained in the FileSet) will be considered and managed.


                        //--- 2nd step : read current buffer file from starting position ------------------------------------------------------

                        List<string> payloadStr = new List<string>();
                        long payloadSizeByte = 0;
                        bool isSizeLimitOverflow = false;
                        bool currentBufferFileHasMoreLines = true;
                        bool bufferFileChanged = false;

                        using (FileStream currentBufferFile = System.IO.File.Open(currentFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            currentBufferFile.Position = nextLineBeginsAtOffset;

                            string nextLine;
                            long nextlineSizeByte;
                            bool continueAdding = true;
                            long previousLineBeginsAtOffset;   

                            while (payloadStr.Count < this._batchPostingLimit && continueAdding && currentBufferFileHasMoreLines)
                            {
                                previousLineBeginsAtOffset = nextLineBeginsAtOffset;

                                // Is there a next line to send? ...
                                if (GoogleCloudPubSubLogShipper.TryReadLine(currentBufferFile, ref nextLineBeginsAtOffset, out nextLine, out nextlineSizeByte))
                                {

                                    // Is there space enough to send the next line in this batch? ...
                                    if (this._batchSizeLimitBytes == null || 
                                        (payloadSizeByte + nextlineSizeByte <= this._batchSizeLimitBytes.Value))
                                    {
                                        //---The next line is added ------------------
                                        this.ActionAddLineToPayload(payloadStr, ref payloadSizeByte, nextLine, nextlineSizeByte);
                                        //--------------------------------------------
                                    }
                                    else
                                    {
                                        isSizeLimitOverflow = true;  // We have reached the size limit for this batch.

                                        if (nextlineSizeByte > this._batchSizeLimitBytes.Value)
                                        {
                                            try
                                            {
                                                // If the line is bigger than the max size for the batch then it is skipped and an error is saved (this
                                                // line will never be sent and would stop sending following lines in an infinite bucle).
                                                this.ActionSkipLine(nextLine, nextlineSizeByte);
                                            }
                                            catch
                                            {
                                            }
                                        }
                                        else
                                        {
                                            // The line has to be processed with the next batch, so we modify the offset to be stored with the mark
                                            // if the current batch data is sent correctly.
                                            this.ActionMoveLineBackwards(ref continueAdding, ref nextLineBeginsAtOffset, previousLineBeginsAtOffset);
                                        }
                                    }

                                }
                                else
                                {
                                    // There is no more data to add to this batch for the current file.
                                    currentBufferFileHasMoreLines = false;
                                }

                            } //---end:while---

                        }


                        if (payloadStr.Count == this._batchPostingLimit || isSizeLimitOverflow)
                        {
                            //  ...we have reached the posting limit.
                            //  ...we have reached the size limit.
                            this._state.DebugOverflow(CNST_Shipper_Debug, payloadStr.Count, this._batchPostingLimit, payloadSizeByte, this._batchSizeLimitBytes);
                            this.overflowsForCurrentFile++;
                        }


                        //--- 3rd step : send data (if we have any) and update bookmark ------------------------------------------------------

                        // Do we have data to send?
                        if (payloadStr.Count > 0)
                        {
                            // ...yes, we have data, so come on...

                            this._state.Debug($"{GoogleCloudPubSubLogShipper.CNST_Shipper_Debug} Payload to send ({payloadStr.Count} lines, {payloadSizeByte} bytes)...", payloadStr);

                            //--- Sending data to Google PubSub... ------------
                            GoogleCloudPubSubClientResponse response = this._state.Publish(payloadStr);
                            //-----------------------------------------------

                            if (response.Success)
                            {
                                //--- OK ---
                                this.ActionMarkBatchSentOK(bookmark, nextLineBeginsAtOffset, currentFilePath, payloadStr);
                            }
                            else
                            {
                                //--- ERROR ---
                                this.ActionMarkBatchSentERROR(response.ErrorMessage, payloadStr);
                                
                                // The while bucle is broken and we will exit the current tick.
                                // The scheduler will se that there has been an error so will act taking it in mind
                                // when calculating next tick.
                                break;
                            }
                           
                        }
                        else if (isSizeLimitOverflow)
                        {
                            // ...no, we don't have data to send, but...

                            // We don't have sent anything to PubSub but an overflow has been detected. This means that there was almost
                            // one line to send and all were bigger than the size limit.
                            // In this case we have to update the bookmark too: if not then next tick will start with this same big line.
                            this.ActionMarkNoBatchButGoForward(bookmark, nextLineBeginsAtOffset, currentFilePath);
                        }
                        else
                        {
                            // ...no, we don't have data to send, but may be there is another buffer file waiting with data to be sent...

                            // For whatever reason, there's nothing waiting to send. This means we should try connecting again at the
                            // regular interval, so mark the attempt as successful.
                            this.ActionMarkNoBatchButSuccess();

                            // Only advance the bookmark if no other process has the current file locked, and its length is as we found it
                            // and there is another next file.
                            bufferFileChanged = this.ActionGoToNextBufferFileIfAny(bookmark, currentFilePath, currentFileSetPosition, nextLineBeginsAtOffset, fileSet);
                        }


                        //--- Retained File Count Limit --------------
                        // If necessary, one obsolete file is deleted each time.
                        // It is done even there is or not data to send: it is possible that our application is sending data at any time.
                        this.ActionDoRetainedFile(currentFileSetPosition, fileSet);
                        //--------------------------------------------


                        // Batch data sent will be done while it is supposed to be more data to send...
                        //  ...because we have reached the posting limit.
                        //  ...because we have reached the size limit.
                        //  ...because we have changed to a new buffer file.
                        // If we go forward with current file but it hasn't got more lines then nothing wrong happens: next iteration will
                        // dtect that it hasn't got more information and it will produce to look for a next buffer file.
                        continueWhile = ((payloadStr.Count == this._batchPostingLimit || isSizeLimitOverflow) && currentBufferFileHasMoreLines) || bufferFileChanged;

                    }
                }
                while (continueWhile);

            }
            catch (Exception ex)
            {
                string auxMessage = $"{CNST_Shipper_Error} Exception while emitting periodic batch. [{ex.Message}]";
                SelfLog.WriteLine(auxMessage);
                this._state.Error(auxMessage);

                this._connectionSchedule.MarkFailure();
            }
            finally
            {
                lock (this._stateLock)
                {
                    if (this._unloading)
                    {
                        try
                        {
                            this._LogCurrentFile(currentFilePath);
                        }
                        catch (Exception)
                        {
                        }                       
                    }
                    else
                    {
                        SetTimer();
                    }
                }
            }
        }
        #endregion




        //*******************************************************************
        //      FILES MANAGEMENT FUNCTIONS (static)
        //*******************************************************************

        #region

        static bool IsUnlockedAtLength(string file, long maxLen, GoogleCloudPubSubSinkState state)
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
                    string auxMessage = $"{CNST_Shipper_Error} Unexpected I/O exception while testing locked status of {file}: {ex.Message}";
                    SelfLog.WriteLine(auxMessage);
                    state.Error(auxMessage);
                }
            }
            catch (Exception ex)
            {
                string auxMessage = $"{CNST_Shipper_Error} Unexpected exception while testing locked status of {file}: {ex.Message}";
                SelfLog.WriteLine(auxMessage);
                state.Error(auxMessage);
            }

            return false;
        }

        static bool TryReadLine(Stream current, ref long nextStart, out string nextLine, out long nextlineSizeByte)
        {
            //bool includesBom = (nextStart == 0);
            nextlineSizeByte = 0;

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

            nextlineSizeByte = Encoding.UTF8.GetByteCount(nextLine) + CNST_NewLineBytes;
            nextStart += nextlineSizeByte;

            //if (includesBom)
            //    nextStart += 3;

            return true;
        }

        static string[] GetFileSet(string logFolder, string candidateSearchPath)
        {
            return Directory.GetFiles(logFolder, candidateSearchPath)
                .OrderBy(n => n)
                .ToArray();
        }

        static bool GetFileSetPosition(string[] fileSet, string filePath, out int fileSetPosition)
        {
            fileSetPosition = 0;

            if (fileSet != null)
            {
                foreach (string f in fileSet)
                {
                    if (fileSet[fileSetPosition] == filePath)
                    {
                        return true;
                    }

                    fileSetPosition++;
                }
            }

            return false;
        }

        #endregion



        //*******************************************************************
        //      BOOKMARK MANAGEMENT (static)
        //*******************************************************************

        #region
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

        static void WriteBookmark(FileStream bookmark, long nextLineBeginsAtOffset, string currentFile)
        {
            using (var writer = new StreamWriter(bookmark))
            {
                writer.WriteLine("{0}:::{1}", nextLineBeginsAtOffset, currentFile);
            }
        }

        #endregion


        //*******************************************************************
        //      SUB-ACTIONS
        //*******************************************************************

        #region
        
        private void ActionAddLineToPayload(List<string> payloadStr, ref long payloadSizeByte, string nextLine, long nextlineSizeByte)
        {
            payloadStr.Add(nextLine);
            payloadSizeByte += nextlineSizeByte;
        }

        private void ActionSkipLine(string nextLine, long nextlineSizeByte)
        {
            string auxMessage = $"{GoogleCloudPubSubLogShipper.CNST_Shipper_Error} Line skipped because it is bigger ({nextlineSizeByte}) than BatchSizeLimitBytes ({this._batchSizeLimitBytes.Value}).";
            SelfLog.WriteLine(auxMessage);
            this._state.Error(auxMessage);
            this._state.DebugEventSkip(GoogleCloudPubSubLogShipper.CNST_Shipper_Debug, nextLine);
            this.linesDroppedForCurrentFile++;
        }

        private void ActionMoveLineBackwards(ref bool continueAdding, ref long nextLineBeginsAtOffset, long previousBeginsAtOffset)
        {
            continueAdding = false;
            nextLineBeginsAtOffset = previousBeginsAtOffset;
        }

        private void ActionMarkBatchSentOK(FileStream bookmark, long nextLineBeginsAtOffset, string currentFilePath, List<string> payloadStr)
        {
            GoogleCloudPubSubLogShipper.WriteBookmark(bookmark, nextLineBeginsAtOffset, currentFilePath);
            this._connectionSchedule.MarkSuccess();
            //---
            this._state.Debug($"{GoogleCloudPubSubLogShipper.CNST_Shipper_Debug} OK sending data to PubSub.");
            //---
            this.linesSentOKForCurrentFile += payloadStr.Count;
            this.batchesSentOKForCurrentFile++;
        }

        private void ActionMarkBatchSentERROR(string errorMessage, List<string> payloadStr)
        {
            this._connectionSchedule.MarkFailure();
            //--
            string auxMessage = $"{GoogleCloudPubSubLogShipper.CNST_Shipper_Error} ERROR sending data to PubSub. [{errorMessage}]";
            SelfLog.WriteLine(auxMessage);
            this._state.Error(auxMessage, payloadStr);
            //---
            this.linesSentERRORForCurrentFile += payloadStr.Count;
            this.batchesSentERRORForCurrentFile++;
        }

        private void ActionMarkNoBatchButGoForward(FileStream bookmark, long nextLineBeginsAtOffset, string currentFilePath)
        {
            GoogleCloudPubSubLogShipper.WriteBookmark(bookmark, nextLineBeginsAtOffset, currentFilePath);
            this._connectionSchedule.MarkSuccess();
        }

        private void ActionMarkNoBatchButSuccess()
        {
            this._connectionSchedule.MarkSuccess();
        }

        private bool ActionGoToNextBufferFileIfAny(FileStream bookmark, string currentFilePath, int currentFileSetPosition, long nextLineBeginsAtOffset, string[] fileSet)
        {
            int nextFileSetPosition = currentFileSetPosition + 1;
            if (nextFileSetPosition < fileSet.Length && GoogleCloudPubSubLogShipper.IsUnlockedAtLength(currentFilePath, nextLineBeginsAtOffset, this._state))
            {
                // --- Move to next file --------------------------------------------------

                try
                {
                    this._LogCurrentFile(currentFilePath);
                    this._state.DebugFileAction($"{GoogleCloudPubSubLogShipper.CNST_Shipper_Debug} Move forward to next file. [{fileSet[nextFileSetPosition]}].");
                }
                catch (Exception)
                {
                }

                GoogleCloudPubSubLogShipper.WriteBookmark(bookmark, 0, fileSet[nextFileSetPosition]);
                //---
                this.ActionInitializeCurrentFileCounters();
                //---
                return true; //We force to manage the new file immediately.
            }
            return false; //We continue with the same buffer file.
        }

        private void ActionInitializeCurrentFileCounters()
        {
            this.linesSentOKForCurrentFile = 0;
            this.linesSentERRORForCurrentFile = 0;
            this.linesDroppedForCurrentFile = 0;
            this.overflowsForCurrentFile = 0;
            this.batchesSentOKForCurrentFile = 0;
            this.batchesSentERRORForCurrentFile = 0;
        }

        private void ActionDoRetainedFile(int currentFileSetPosition, string[] fileSet)
        {
            if (fileSet.Length > 1 && fileSet.Length > this._retainedFileCountLimit && currentFileSetPosition > 0)
            {
                this._state.DebugFileAction($"{GoogleCloudPubSubLogShipper.CNST_Shipper_Debug} File delete. [{fileSet[0]}].");
                System.IO.File.Delete(fileSet[0]);
            }
        }

        private void _LogCurrentFile(string currentFilePath)
        {
            string auxMessage = string.Format("{0} File finished: LinesOK=[{1}] LinesERROR=[{2}] LinesSkiped=[{3}] BatchesOK=[{4}] BatchesERROR=[{5}] Overflows=[{6}] File=[{7}]",
                                              GoogleCloudPubSubLogShipper.CNST_Shipper_Debug,
                                              this.linesSentOKForCurrentFile,
                                              this.linesSentERRORForCurrentFile,
                                              this.linesDroppedForCurrentFile,
                                              this.batchesSentOKForCurrentFile,
                                              this.batchesSentERRORForCurrentFile,
                                              this.overflowsForCurrentFile,
                                              currentFilePath
                                              );
            this._state.DebugFileAction(auxMessage);
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