﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Threading;
using System.ServiceModel;
using System.Drawing;
using System.IO;
using System.Collections.ObjectModel;
using Emulators.Database;
using Emulators.Scrapers;
using Emulators.ImageHandlers;

namespace Emulators.Import
{
    #region EventArgs
    public class ImportStatusEventArgs : EventArgs
    {
        public ImportStatusEventArgs(ImportAction action, List<RomMatch> newItems)
        {
            NewItems = newItems;
            Action = action;
        }

        public List<RomMatch> NewItems { get; protected set; }
        public ImportAction Action { get; protected set; }
    }

    public class RomStatusEventArgs : EventArgs
    {
        public RomStatusEventArgs(RomMatch romMatch, RomMatchStatus status)
        {
            RomMatch = romMatch;
            Status = status;
        }

        public RomMatch RomMatch { get; protected set; }
        public RomMatchStatus Status { get; protected set; }
    }

    public class ImportProgressEventArgs : EventArgs
    {
        public ImportProgressEventArgs(int percent, int current, int total, string description)
        {
            Percent = percent;
            Current = current;
            Total = total;
            Description = description;
        }

        public int Percent { get; protected set; }
        public int Current { get; protected set; }
        public int Total { get; protected set; }
        public string Description { get; protected set; }
    }
    #endregion

    public class Importer
    {
        int threadCount = 5;
        readonly object syncRoot = new object();

        volatile bool doWork = false;
        volatile bool pause = false;

        readonly object lookupSync = new object();
        Dictionary<int, RomMatch> lookupMatch;

        List<Thread> importerThreads;
        volatile int percentDone = 0;

        bool isBackground = false;
        bool justRefresh = false;
        ScraperProvider scraperProvider = null;

        #region Constructors

        public Importer()
        {
            init();
        }
        
        public Importer(bool isBackground, bool justRefresh = false)
        {
            this.isBackground = isBackground;
            this.justRefresh = justRefresh;
            init();
        }

        #endregion

        #region Events

        //Sends progress updates
        public event EventHandler<ImportProgressEventArgs> ProgressChanged;
        protected virtual void OnProgressChanged(ImportProgressEventArgs e)
        {
            if (ProgressChanged != null)
                ProgressChanged(this, e);
        }
        //Sends import status events
        public event EventHandler<ImportStatusEventArgs> ImportStatusChanged;
        protected virtual void OnImportStatusChanged(ImportStatusEventArgs e)
        {
            if (ImportStatusChanged != null)
                ImportStatusChanged(this, e);
        }
        //Sends item update events
        public event EventHandler<RomStatusEventArgs> RomStatusChanged;
        protected virtual void OnRomStatusChanged(RomStatusEventArgs e)
        {
            if (RomStatusChanged != null)
                RomStatusChanged(this, e);
        }

        void setRomStatus(RomMatch romMatch, RomMatchStatus status)
        {
            if (ImporterStatus == ImportAction.ImportRestarting)
                return;
            OnRomStatusChanged(new RomStatusEventArgs(romMatch, status));
        }

        void scanProgress(string message)
        {
            UpdatePercentDone();
            int processed = lookupMatch.Count - pendingMatches.Count - priorityPendingMatches.Count;
            int total = lookupMatch.Count;
            OnProgressChanged(new ImportProgressEventArgs(percentDone, processed, total, message));
        }

        private void retrieveProgress(string message)
        {
            UpdatePercentDone();
            int total = lookupMatch.Count - matchesNeedingInput.Count;
            int processed = total - approvedMatches.Count - priorityApprovedMatches.Count - pendingMatches.Count - priorityPendingMatches.Count;
            OnProgressChanged(new ImportProgressEventArgs(percentDone, processed, total, message));
        }

        private void UpdatePercentDone()
        {
            // calculate the total actions that need to be performed this session
            int mediaToScan = lookupMatch.Count;
            int mediaToCommit = lookupMatch.Count - matchesNeedingInput.Count;
            int totalActions = mediaToScan + mediaToCommit;

            // if there is nothing to do, set progress to 100%
            if (totalActions == 0)
            {
                percentDone = 100;
                return;
            }

            // calculate the number of actions completed so far
            int mediaScanned = lookupMatch.Count - pendingMatches.Count - priorityPendingMatches.Count;
            int mediaCommitted = commitedMatches.Count;

            percentDone = (int)Math.Round(((double)mediaScanned + mediaCommitted) * 100 / ((double)totalActions));

            if (percentDone > 100)
                percentDone = 100;
        }

        #endregion

        #region Public Properties

        public bool JustRefresh
        {
            get { return justRefresh; }
            set { justRefresh = value; }
        }

        object importStatusLock = new object();
        ImportAction importerStatus = ImportAction.ImportStopped;
        public ImportAction ImporterStatus
        {
            get
            {
                lock (importStatusLock)
                    return importerStatus;
            }
            protected set
            {
                lock (importStatusLock)
                {
                    if (importerStatus == value || (importerStatus == ImportAction.ImportRestarting && value != ImportAction.ImportStarting))
                        return;
                    importerStatus = value;
                    OnImportStatusChanged(new ImportStatusEventArgs(importerStatus, null));
                }
            }
        }

        public ReadOnlyCollection<RomMatch> AllMatches
        {
            get 
            {
                lock (lookupSync)
                    return new List<RomMatch>(lookupMatch.Values).AsReadOnly();
            }
        }

        // Matches that have not yet been scraped.
        public ReadOnlyCollection<RomMatch> PendingMatches
        {
            get { return pendingMatches.AsReadOnly(); }
        } private List<RomMatch> pendingMatches;

        // Same as PendingMatches, but this list gets priority. Used for user based interaction.
        public ReadOnlyCollection<RomMatch> PriorityPendingMatches
        {
            get { return priorityPendingMatches.AsReadOnly(); }
        } private List<RomMatch> priorityPendingMatches;

        // Matches that are not close enough for auto approval and require user input.
        public ReadOnlyCollection<RomMatch> MatchesNeedingInput
        {
            get { return matchesNeedingInput.AsReadOnly(); }
        } private List<RomMatch> matchesNeedingInput;

        // Matches that are accepted and are awaiting details retrieval and commital. 
        public ReadOnlyCollection<RomMatch> ApprovedMatches
        {
            get { return approvedMatches.AsReadOnly(); }
        } private List<RomMatch> approvedMatches;

        // Same as ApprovedMatches but this list get's priority. Used for user based interaction.
        public ReadOnlyCollection<RomMatch> PriorityApprovedMatches
        {
            get { return priorityApprovedMatches.AsReadOnly(); }
        } private List<RomMatch> priorityApprovedMatches;

        // Matches that have been ignored/committed and saved to the database. 
        public ReadOnlyCollection<RomMatch> CommitedMatches
        {
            get { return commitedMatches.AsReadOnly(); }
        } private List<RomMatch> commitedMatches;

        #endregion

        #region Start/Stop

        void init()
        {
            lock (syncRoot)
            {
                threadCount = EmulatorsCore.Options.ReadOption(o => o.ImportThreads);
                if (threadCount < 1) //0 threads will take a very long time to complete :)
                    threadCount = 1;

                lookupMatch = new Dictionary<int, RomMatch>();
                pendingMatches = new List<RomMatch>();
                priorityPendingMatches = new List<RomMatch>();
                matchesNeedingInput = new List<RomMatch>();
                approvedMatches = new List<RomMatch>();
                priorityApprovedMatches = new List<RomMatch>();
                commitedMatches = new List<RomMatch>();

                importerThreads = new List<Thread>();
                scraperProvider = new ScraperProvider();
                scraperProvider.DoWork += new DoWorkDelegate(() => doWork);
            }
        }

        public void Start()
        {
            lock (syncRoot)
            {
                if (importerThreads.Count != 0)
                    return;

                ImporterStatus = ImportAction.ImportStarting;
                doWork = true;
                if (justRefresh)
                    doRefreshTasks();
                else
                    doImportTasks();
            }
        }

        void doRefreshTasks()
        {
            Thread refreshThread = new Thread(new ThreadStart(delegate()
            {
                ImporterStatus = ImportAction.ImportRefreshing;
                refreshDatabase();
                ImporterStatus = ImportAction.ImportFinished;
            })) { Name = "Importer Thread" };
            importerThreads.Add(refreshThread);
            refreshThread.Start();
        }

        void doImportTasks()
        {
            Action scanMethod;
            if (isBackground)
                scanMethod = scanRomsBackground; //retrieve info as soon as match found
            else
                scanMethod = scanRomsDefault; //get all possible matches before retrieving so user can check/edit

            Thread firstThread = new Thread(new ThreadStart(delegate()
            {
                refreshDatabase();
                if (!doWork)
                    return;
                populatePendingMatches(); //first thread needs to locate games for import
                if (doWork)
                {
                    ImporterStatus = ImportAction.ImportStarted;
                    scanRoms(scanMethod, true);
                }
                ImporterStatus = ImportAction.ImportFinished;
            })) { Name = "Importer Thread 1" };
            firstThread.Start();
            importerThreads.Add(firstThread);
            //start rest of threads
            for (int x = 2; x <= threadCount; x++)
            {
                Thread thread = new Thread(new ThreadStart(delegate() { scanRoms(scanMethod, false); })) { Name = "Importer Thread " + x };
                thread.Start();
                importerThreads.Add(thread);
            }
        }

        public void Stop()
        {
            lock (syncRoot)
            {
                ImporterStatus = ImportAction.ImportStopping;
                doWork = false;

                if (importerThreads.Count > 0)
                {
                    Logger.LogInfo("Shutting Down Importer Threads...");
                    // wait for all threads to shut down
                    bool waiting = true;
                    while (waiting)
                    {
                        waiting = false;
                        foreach (Thread currThread in importerThreads)
                            waiting = waiting || currThread.IsAlive;
                        Thread.Sleep(100);
                    }
                    importerThreads.Clear();
                }

                OnProgressChanged(new ImportProgressEventArgs(100, 0, 0, "Stopped"));
                ImporterStatus = ImportAction.ImportStopped;
                Logger.LogInfo("Stopped Importer");
            }
        }

        public void Restart()
        {
            lock (importStatusLock)
            {
                if (importerStatus == ImportAction.ImportRestarting)
                    return;
                ImporterStatus = ImportAction.ImportRestarting;
            }
            new Thread(delegate()
                {
                    lock (syncRoot)
                    {
                        Stop();
                        init();
                        Start();
                    }
                }) { Name = "Importer restarter", IsBackground = true }.Start();
        }

        public void Pause()
        {
            pause = true;
        }

        public void UnPause()
        {
            pause = false;
        }

        #endregion

        #region Public Methods

        public void AddGames(IEnumerable<Game> games)
        {
            lock (importStatusLock)
            {
                List<int> gameIds = new List<int>();
                EmulatorsCore.Database.ExecuteTransaction(games, game =>
                {
                    if (game == null)
                        return;

                    if (!game.Id.HasValue)
                        game.Commit();
                    else if (gameIds.Contains(game.Id.Value))
                        return;

                    gameIds.Add(game.Id.Value);
                    RomMatch romMatch;
                    lock (lookupSync)
                    {
                        if (lookupMatch.ContainsKey(game.Id.Value))
                        {
                            romMatch = lookupMatch[game.Id.Value];
                            lock (romMatch.SyncRoot)
                            {
                                RemoveFromMatchLists(romMatch);
                                romMatch.CurrentThreadId = null;
                                romMatch.Status = RomMatchStatus.Removed;
                            }
                        }
                        game.Reset();
                        romMatch = new RomMatch(game);
                        lookupMatch[game.Id.Value] = romMatch;
                    }

                    game.InfoChecked = false;
                    game.SaveInfoCheckedStatus();
                    setRomStatus(romMatch, RomMatchStatus.PendingMatch);
                    lock (priorityPendingMatches)
                        priorityPendingMatches.Add(romMatch);

                });

                if (importerStatus != ImportAction.ImportRestarting && importerStatus != ImportAction.ImportStarted)
                {
                    justRefresh = false;
                    Restart();
                }
            }
        }
        /// <summary>
        /// Resets RomMatch status and re-retrieves info
        /// </summary>
        /// <param name="romMatch"></param>
        public void ReProcess(RomMatch romMatch)
        {
            if (romMatch == null)
                return;

            lock (lookupSync)
            {
                if (lookupMatch.ContainsKey(romMatch.ID))
                    romMatch = lookupMatch[romMatch.ID];
                lock (romMatch.SyncRoot)
                {
                    RemoveFromMatchLists(romMatch);
                    romMatch.Status = RomMatchStatus.PendingMatch;
                    romMatch.HighPriority = true;
                    romMatch.CurrentThreadId = null;
                    romMatch.GameDetails = null;
                    romMatch.PossibleGameDetails = new List<ScraperResult>();
                }
                lookupMatch[romMatch.ID] = romMatch;
            }
            romMatch.Game.InfoChecked = false;
            romMatch.Game.SaveInfoCheckedStatus();
            setRomStatus(romMatch, RomMatchStatus.PendingMatch);
            lock (priorityPendingMatches)
                priorityPendingMatches.Add(romMatch);
        }

        public void Approve(RomMatch romMatch)
        {
            Approve(new RomMatch[] { romMatch });
        }
        /// <summary>
        /// Approve a RomMatch requiring input and queue downloading selected info
        /// </summary>
        /// <param name="romMatches"></param>
        public void Approve(IEnumerable<RomMatch> romMatches)
        {
            EmulatorsCore.Database.ExecuteTransaction(romMatches, romMatch =>
            {
                if (romMatch.PossibleGameDetails == null || romMatch.PossibleGameDetails.Count < 1 || !romMatch.Game.Id.HasValue)
                    return;

                int gameId = romMatch.Game.Id.Value;
                lock (lookupSync)
                {
                    if (lookupMatch.ContainsKey(gameId))
                    {
                        romMatch = lookupMatch[gameId];
                        lock (romMatch.SyncRoot)
                        {
                            if (romMatch.Status != RomMatchStatus.Approved && romMatch.Status != RomMatchStatus.NeedsInput)
                                return;

                            RemoveFromMatchLists(romMatch);
                            romMatch.Status = RomMatchStatus.Approved;
                            romMatch.CurrentThreadId = null;
                            romMatch.HighPriority = true;
                        }
                        romMatch.Game.InfoChecked = false;
                        romMatch.Game.SaveInfoCheckedStatus();
                        lookupMatch[romMatch.ID] = romMatch;
                        setRomStatus(romMatch, RomMatchStatus.Approved);
                        lock (priorityApprovedMatches)
                            priorityApprovedMatches.Add(romMatch);
                    }
                }
            });
        }

        public void UpdateSelectedMatch(RomMatch romMatch, ScraperResult newResult)
        {
            if (newResult == null)
                return;
            lock (lookupSync)
            {
                if (lookupMatch.ContainsKey(romMatch.ID))
                {
                    romMatch = lookupMatch[romMatch.ID];
                    lock (romMatch.SyncRoot)
                    {
                        RemoveFromMatchLists(romMatch);
                        romMatch.Status = RomMatchStatus.Approved;
                        romMatch.CurrentThreadId = null;
                        romMatch.HighPriority = true;
                    }
                    romMatch.GameDetails = newResult;
                    romMatch.Game.InfoChecked = false;
                    romMatch.Game.SaveInfoCheckedStatus();
                    lookupMatch[romMatch.ID] = romMatch;
                    lock (priorityApprovedMatches)
                        priorityApprovedMatches.Add(romMatch);
                    setRomStatus(romMatch, RomMatchStatus.Approved);
                }
            }
        }

        public void Ignore(RomMatch romMatch)
        {
            Ignore(new RomMatch[] { romMatch });
        }
        /// <summary>
        /// Removes the RomMatch from the Importer
        /// </summary>
        /// <param name="romMatch"></param>
        public void Ignore(IEnumerable<RomMatch> romMatches)
        {
            EmulatorsCore.Database.ExecuteTransaction(romMatches, romMatch =>
            {
                lock (lookupSync)
                {
                    if (lookupMatch.ContainsKey(romMatch.ID))
                    {
                        RomMatch lMatch = lookupMatch[romMatch.ID];
                        lock (lMatch.SyncRoot)
                        {
                            RemoveFromMatchLists(lMatch);
                            lMatch.Status = RomMatchStatus.Ignored;
                            lMatch.CurrentThreadId = null;
                        }
                        lMatch.Game.Reset();
                        lMatch.Game.InfoChecked = true;
                        lMatch.Game.Commit();
                        setRomStatus(lMatch, RomMatchStatus.Ignored);
                    }
                }
            });
        }

        public void Remove(int? gameId)
        {
            if (!gameId.HasValue)
                return;
            RomMatch romMatch;
            lock (lookupSync)
            {
                if (lookupMatch.ContainsKey(gameId.Value))
                {
                    romMatch = lookupMatch[gameId.Value];
                    lock (romMatch.SyncRoot)
                    {
                        RemoveFromMatchLists(romMatch);
                        romMatch.Status = RomMatchStatus.Removed;
                        romMatch.CurrentThreadId = null;
                    }
                    setRomStatus(romMatch, RomMatchStatus.Removed);
                }
            }
        }

        #endregion

        #region Scan Logic
        // The main loop that the import threads will run, checks for pending actions and updates status
        void scanRoms(Action scanMethod, bool raiseEvents)
        {
            try
            {
                while (doWork)
                {
                    checkPauseState(raiseEvents);
                    if (!checkForPendingItems())
                        return;
                    scanMethod.Invoke();
                    UpdatePercentDone();

                    lock (importStatusLock)
                    {
                        if (!doWork)
                            return;
                        if (checkIfWaitingForApprovals())
                        {
                            OnProgressChanged(new ImportProgressEventArgs(percentDone, 0, matchesNeedingInput.Count, "Waiting for Approvals..."));
                            if (isBackground)
                            {
                                doWork = false;
                                ImporterStatus = ImportAction.ImportFinished;
                                return;
                            }
                        }

                        if (checkIfAllItemsProcessed())
                        {
                            UpdatePercentDone();
                            if (percentDone == 100)
                                OnProgressChanged(new ImportProgressEventArgs(100, 0, 0, "Done!"));

                            if (isBackground)
                            {
                                doWork = false;
                                ImporterStatus = ImportAction.ImportFinished;
                                return;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogError("Unhandled error in Importer - {0} - {1}\r\n{2}", e, e.Message, e.StackTrace);
            }
        }

        void checkPauseState(bool raiseEvents)
        {
            if (pause)
            {
                if (raiseEvents)
                    OnImportStatusChanged(new ImportStatusEventArgs(ImportAction.ImportPaused, null));
                while (pause)
                    if (!checkAndWait(100, 10, true))
                        return;
                if (raiseEvents)
                    OnImportStatusChanged(new ImportStatusEventArgs(ImportAction.ImportResumed, null));
            }
        }

        bool checkForPendingItems()
        {
            int previousCommittedCount = commitedMatches.Count;
            int previousMatchesCount = lookupMatch.Count;
            // if there is nothing to process, then sleep
            while (pendingMatches.Count == 0 &&
                   approvedMatches.Count == 0 &&
                   priorityPendingMatches.Count == 0 &&
                   priorityApprovedMatches.Count == 0 &&
                   commitedMatches.Count == previousCommittedCount &&
                   previousMatchesCount == lookupMatch.Count &&
                   doWork
                   )
            {
                if (!checkAndWait(100, 10, false))
                    return false;
            }
            return true;
        }

        bool checkIfWaitingForApprovals()
        {
            return pendingMatches.Count == 0 && approvedMatches.Count == 0 &&
                   priorityPendingMatches.Count == 0 && priorityApprovedMatches.Count == 0 &&
                   lookupMatch.Count == commitedMatches.Count + matchesNeedingInput.Count &&
                   matchesNeedingInput.Count > 0;
        }

        bool checkIfAllItemsProcessed()
        {
            return pendingMatches.Count == 0 && approvedMatches.Count == 0 &&
                   priorityPendingMatches.Count == 0 && priorityApprovedMatches.Count == 0 &&
                   commitedMatches.Count == lookupMatch.Count &&
                   matchesNeedingInput.Count == 0;
        }

        // The order is to have the user selected roms be done first, and then the standard import order second.
        // The order needs to be Hash, Server, Pending Matches, Approved Matches
        void scanRomsDefault()
        {
            if (priorityPendingMatches.Count > 0)
                processNextPendingMatch(true);
            else if (priorityApprovedMatches.Count > 0)
                processNextApprovedMatch(true);
            else if (pendingMatches.Count > 0)
                processNextPendingMatch(false);
            else if (approvedMatches.Count > 0)
                processNextApprovedMatch(false);
        }

        //alternate order when background, get info asap so GUI can be updated
        void scanRomsBackground()
        {
            if (priorityApprovedMatches.Count > 0)
                processNextApprovedMatch(true);
            else if (priorityPendingMatches.Count > 0)
                processNextPendingMatch(true);
            else if (approvedMatches.Count > 0)
                processNextApprovedMatch(false);
            else if (pendingMatches.Count > 0)
                processNextPendingMatch(false);
        }
        #endregion

        #region Refresh Database

        void populatePendingMatches()
        {
            List<RomMatch> localMatches = getGamesNeedingImport();
            if (localMatches.Count == 0)
            {
                if (isBackground)
                    doWork = false;
                else
                    OnImportStatusChanged(new ImportStatusEventArgs(ImportAction.NoFilesFound, null));
                return;
            }

            Logger.LogDebug("Adding {0} items to importer", localMatches.Count);
            lock (lookupSync)
            {
                foreach (RomMatch romMatch in localMatches)
                {
                    if (!lookupMatch.ContainsKey(romMatch.ID))
                    {
                        lookupMatch[romMatch.ID] = romMatch;
                        lock (pendingMatches)
                            pendingMatches.Add(romMatch);
                    }
                }
            }

            OnImportStatusChanged(new ImportStatusEventArgs(ImportAction.PendingFilesAdded, localMatches));
            OnProgressChanged(new ImportProgressEventArgs(0, 0, 0, "Ready"));
        }

        List<RomMatch> getGamesNeedingImport()
        {
            List<RomMatch> matches = new List<RomMatch>();
            foreach (Game game in Game.GetAll(false))
            {
                game.Reset();
                matches.Add(new RomMatch(game));
            }
            return matches;
        }

        void refreshDatabase()
        {
            DBRefresh dbRefresh = new DBRefresh();
            dbRefresh.DeleteMissingGames();
            if (dbRefresh.RefreshDatabase())
                OnImportStatusChanged(new ImportStatusEventArgs(ImportAction.NewFilesFound, null));
        }

        #endregion

        #region Pending Matches
        /// <summary>
        /// Selects the next pending match and searches for possible game matches, if a close match is found the RomMatch
        /// is added to the Approve queue else it is added to the matches needing input queue.
        /// </summary>
        private void processNextPendingMatch(bool priorityOnly)
        {
            RomMatch romMatch = takeFromList(pendingMatches, priorityPendingMatches, priorityOnly);
            if (romMatch == null)
                return;

            if (!romMatch.Game.IsMissingInfo())
            {
                lock (romMatch.SyncRoot)
                {
                    if (!romMatch.OwnedByThread())
                        return;

                    romMatch.Game.InfoChecked = true;
                    romMatch.Game.Commit();
                    romMatch.Status = RomMatchStatus.Committed;
                    lock (commitedMatches)
                    {
                        commitedMatches.Add(romMatch);
                        setRomStatus(romMatch, RomMatchStatus.Committed);
                    }
                }
                return;
            }
            
            scanProgress("Retrieving matches: " + romMatch.Path);
            ScraperResult bestResult; bool approved;
            List<ScraperResult> results = scraperProvider.GetMatches(romMatch, out bestResult, out approved);

            RomMatchStatus status;
            List<RomMatch> addList;
            List<RomMatch> priorityAddList;
            lock (romMatch.SyncRoot)
            {
                if (!romMatch.OwnedByThread())
                    return;

                romMatch.PossibleGameDetails = results;
                romMatch.GameDetails = bestResult;
                if (approved) //close match found, add to approve list
                {
                    status = RomMatchStatus.Approved;
                    addList = approvedMatches;
                    priorityAddList = priorityApprovedMatches;
                }
                else //close match not found, request user input
                {
                    status = RomMatchStatus.NeedsInput;
                    addList = matchesNeedingInput;
                    priorityAddList = null;
                }
            }
            addToList(romMatch, status, addList, priorityAddList);
        }
        #endregion

        #region Approved Matches

        //Selects next Match from approved match lists.
        //Updates the Game with the specified Match details and commits
        void processNextApprovedMatch(bool priorityOnly)
        {
            RomMatch romMatch = takeFromList(approvedMatches, priorityApprovedMatches, priorityOnly);
            if (romMatch == null)
                return;
            Scraper selectedScraper;
            ScraperResult selectedMatch;
            lock (romMatch.SyncRoot)
            {
                if (!romMatch.OwnedByThread())
                    return;
                selectedScraper = romMatch.GameDetails.DataProvider;
                selectedMatch = romMatch.GameDetails;
            }

            if (!doWork)
                return;

            retrieveProgress("Updating: " + romMatch.Path);
            ScraperGame scraperGame = scraperProvider.DownloadInfo(selectedMatch);
            if (!doWork || !romMatch.OwnedByThread())
                return;

            using (ThumbGroup thumbGroup = new ThumbGroup(romMatch.Game))
            {
                using (SafeImage image = getImage(scraperGame.BoxFrontUrl, romMatch))
                    if (!saveImage(image, romMatch, thumbGroup, ThumbType.FrontCover))
                        return;

                using (SafeImage image = getImage(scraperGame.BoxBackUrl, romMatch))
                    if (!saveImage(image, romMatch, thumbGroup, ThumbType.BackCover))
                        return;

                using (SafeImage image = getImage(scraperGame.TitleScreenUrl, romMatch))
                    if (!saveImage(image, romMatch, thumbGroup, ThumbType.TitleScreen))
                        return;

                using (SafeImage image = getImage(scraperGame.InGameUrl, romMatch))
                    if (!saveImage(image, romMatch, thumbGroup, ThumbType.InGameScreen))
                        return;

                using (SafeImage image = getImage(scraperGame.FanartUrl, romMatch))
                    if (!saveImage(image, romMatch, thumbGroup, ThumbType.Fanart))
                        return;
            }

            lock (romMatch.SyncRoot)
            {
                if (!romMatch.OwnedByThread())
                    return;
                romMatch.ScraperGame = scraperGame;
                commitGame(romMatch);
                if (!doWork)
                    return;
            }
            addToList(romMatch, RomMatchStatus.Committed, commitedMatches, null);
        }

        bool saveImage(SafeImage image, RomMatch romMatch, ThumbGroup thumbGroup, ThumbType thumbType)
        {
            if (!doWork)
                return false;
            lock (romMatch.SyncRoot)
            {
                if (!romMatch.OwnedByThread())
                    return false;
                if (image != null)
                {
                    thumbGroup.GetThumbObject(thumbType).SetSafeImage(image.Image);
                    thumbGroup.SaveThumb(thumbType);
                }
            }
            return true;
        }

        SafeImage getImage(string url, RomMatch romMatch)
        {
            if (string.IsNullOrEmpty(url))
                return null;
            BitmapDownloadResult result = ImageHandler.BeginBitmapFromWeb(url);
            if (result == null)
                return null;

            while (!result.IsCompleted)
            {
                if (!doWork || !romMatch.OwnedByThread())
                {
                    result.Cancel();
                    return null;
                }
                Thread.Sleep(100);
            }
            return result.SafeImage;
        }

        //update and commit game
        void commitGame(RomMatch romMatch)
        {
            Game dbGame = EmulatorsCore.Database.Get<Game>(romMatch.ID);
            if (dbGame == null)
                return; //game deleted

            ScraperGame details = romMatch.ScraperGame;
            if (details == null)
                return;

            dbGame.Title = details.Title == null ? "" : details.Title;
            dbGame.Developer = details.Company == null ? "" : details.Company;
            dbGame.Description = details.Description == null ? "" : details.Description;
            dbGame.Genre = details.Genre == null ? "" : details.Genre;
            
            int year;
            if (!int.TryParse(details.Year, out year))
                year = 0;
            dbGame.Year = year;
            
            double grade;
            if (!double.TryParse(details.Grade, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out grade))
                grade = 0;
            while (grade > 10)
                grade = grade / 10;
            dbGame.Grade = (int)Math.Round(grade);

            if (!doWork)
                return;

            dbGame.InfoChecked = true;
            dbGame.Commit();
        }

        #endregion

        // removes the given match from all pending process lists
        private void RemoveFromMatchLists(RomMatch match)
        {
            lock (pendingMatches)
                if (pendingMatches.Contains(match))
                    pendingMatches.Remove(match);

            lock (priorityPendingMatches)
                if (priorityPendingMatches.Contains(match))
                    priorityPendingMatches.Remove(match);

            lock (matchesNeedingInput)
                if (matchesNeedingInput.Contains(match))
                    matchesNeedingInput.Remove(match);

            lock (approvedMatches)
                if (approvedMatches.Contains(match))
                    approvedMatches.Remove(match);

            lock (priorityApprovedMatches)
                if (priorityApprovedMatches.Contains(match))
                    priorityApprovedMatches.Remove(match);

            lock (commitedMatches)
                if (commitedMatches.Contains(match))
                    commitedMatches.Remove(match);

            lock (lookupSync)
                if (lookupMatch.ContainsKey(match.ID))
                    lookupMatch.Remove(match.ID);
        }

        RomMatch takeFromList(List<RomMatch> list, List<RomMatch> priorityList, bool priorityOnly)
        {
            RomMatch romMatch = null;
            if (priorityList != null)
            {
                lock (priorityList)
                    if (priorityList.Count > 0)
                    {
                        romMatch = (RomMatch)priorityList[0];
                        romMatch.CurrentThreadId = Thread.CurrentThread.ManagedThreadId;
                        priorityList.Remove(romMatch);
                    }
            }
            if (!priorityOnly && romMatch == null && list != null)
                lock (list)
                    if (list.Count > 0)
                    {
                        romMatch = (RomMatch)list[0];
                        romMatch.CurrentThreadId = Thread.CurrentThread.ManagedThreadId;
                        list.Remove(romMatch);
                    }
            return romMatch;
        }

        void addToList(RomMatch romMatch, RomMatchStatus newStatus, List<RomMatch> list, List<RomMatch> priorityList)
        {
            lock (romMatch.SyncRoot)
            {
                if (!romMatch.OwnedByThread())
                    return;
                romMatch.CurrentThreadId = null;
                romMatch.Status = newStatus;

                if (priorityList != null && romMatch.HighPriority)
                    lock (priorityList)
                        priorityList.Add(romMatch);
                else if (list != null)
                    lock (list)
                        list.Add(romMatch);
                setRomStatus(romMatch, newStatus);
            }
        }

        bool checkAndWait(int interval, int count, bool checkPause)
        {
            if (interval < 1 || count < 1)
                return true;

            for (int x = 0; x < count; x++)
            {
                if (!doWork)
                    return false;
                if (checkPause && !pause)
                    return true;
                Thread.Sleep(interval);
            }
            return true;
        }
    }

    public enum ImportAction
    {
        ImportFinishing,
        ImportFinished,
        ImportStarting,
        ImportStarted,
        ImportStopping,
        ImportStopped,
        ImportRestarting,
        NoFilesFound,
        PendingFilesAdded,
        ImportPaused,
        ImportResumed,
        NewFilesFound,
        ImportRefreshing,
    }
}
