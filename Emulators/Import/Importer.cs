using System;
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
        int hashThreadCount = 2;
        readonly object syncRoot = new object();

        volatile bool doWork = false;
        ManualResetEventSlim doWorkWaitHandle = new ManualResetEventSlim();
        volatile bool pause = false;
        ManualResetEventSlim unPauseWaitHandle = new ManualResetEventSlim(true);

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
                Options options = EmulatorsCore.Options;
                options.EnterReadLock();
                threadCount = options.ImportThreads;
                hashThreadCount = options.HashThreads;
                options.ExitReadLock();

                if (threadCount < 1) //0 threads will take a very long time to complete :)
                    threadCount = 1;
                if (hashThreadCount < 1)
                    hashThreadCount = 1;

                pendingMatches = new List<RomMatch>();
                priorityPendingMatches = new List<RomMatch>();
                matchesNeedingInput = new List<RomMatch>();
                approvedMatches = new List<RomMatch>();
                priorityApprovedMatches = new List<RomMatch>();
                commitedMatches = new List<RomMatch>();

                importerThreads = new List<Thread>();
                lookupMatch = new Dictionary<int, RomMatch>();
                scraperProvider = new ScraperProvider();
                scraperProvider.DoWork += new DoWorkDelegate(() => doWork);
            }
        }

        public void Start()
        {
            lock (syncRoot)
            {
                ImporterStatus = ImportAction.ImportStarting;
                doWork = true;

                if (importerThreads.Count == 0)
                {
                    if (!justRefresh) //start retrieving immediately
                    {
                        Action scanMethod;
                        if (isBackground)
                            scanMethod = scanRomsBackground; //retrieve info as soon as match found
                        else
                            scanMethod = scanRomsDefault; //get all possible matches before retrieving so user can check/edit

                        Thread firstThread = new Thread(new ThreadStart(delegate()
                        {
                            getFilesToImport(); //first thread needs to locate games for import

                            if (doWork)
                            {
                                ImporterStatus = ImportAction.ImportStarted;
                                scanRoms(scanMethod, true);
                            }
                            ImporterStatus = ImportAction.ImportFinished;
                        }
                            ));
                        firstThread.Name = "Importer Thread";
                        firstThread.Start();
                        importerThreads.Add(firstThread);

                        //start rest of threads
                        for (int x = 0; x < threadCount - 1; x++)
                        {
                            Thread thread = new Thread(new ThreadStart(delegate() { scanRoms(scanMethod, false); }));
                            thread.Name = "Importer Thread";
                            thread.Start();
                            importerThreads.Add(thread);
                        }

                    }
                    else //just get list of files to import
                    {
                        Thread firstThread = new Thread(new ThreadStart(delegate()
                        {
                            lock (importStatusLock)
                                ImporterStatus = ImportAction.ImportRefreshing;
                            getFilesToImport();
                            lock (importStatusLock)
                                ImporterStatus = ImportAction.ImportFinished;
                        }
                            ));
                        firstThread.Name = "Importer Thread";
                        importerThreads.Add(firstThread);
                        firstThread.Start();
                    }
                }
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

        //Refreshes the DB and builds list of RomMatches based on Game.IsInfoChecked
        //called first when started
        void getFilesToImport()
        {
            try
            {
                refreshDatabase();
                if (justRefresh || !doWork)
                    return;

                List<RomMatch> localMatches = new List<RomMatch>();
                foreach (Game game in Game.GetAll(false))
                {
                    if (!doWork)
                        return;
                    game.Reset();
                    localMatches.Add(new RomMatch(game));
                }

                Logger.LogDebug("Adding {0} item{1} to importer", localMatches.Count, localMatches.Count == 1 ? "" : "s");
                if (localMatches.Count > 0)
                {
                    setListCapacities((int)(localMatches.Count * 1.5));
                    for (int x = 0; x < localMatches.Count; x++)
                    {
                        RomMatch romMatch = localMatches[x];
                        OnProgressChanged(new ImportProgressEventArgs((x * 100) / localMatches.Count, x, localMatches.Count, string.Format("Importing {0}", romMatch.Path)));
                        lock (lookupSync)
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
                else //no files need importing
                {
                    if (isBackground)
                    {
                        doWork = false;
                        return;
                    }
                    else
                        OnImportStatusChanged(new ImportStatusEventArgs(ImportAction.NoFilesFound, null));
                }
            }
            catch (Exception e)
            {
                Logger.LogError("Unhandled error in Importer - {0} - {1} - {2}", e, e.Message, e.StackTrace);
            }
        }

        void refreshDatabase()
        {
            Logger.LogDebug("Refreshing database");
            OnProgressChanged(new ImportProgressEventArgs(0, 0, 0, "Refreshing database"));

            List<Game> allGames = Game.GetAll();
            deleteMissingGames(allGames);
            if (!doWork) return;

            List<string> dbPaths = EmulatorsCore.Database.GetAll(typeof(GameDisc)).Select(g => ((GameDisc)g).Path).ToList();
            List<Emulator> emus = Emulator.GetAll();
            if (!doWork) return;

            List<Game> newGames = new List<Game>();
            int filesFound = 0;
            //loop through each emu
            foreach (Emulator emu in emus)
            {
                if (!doWork) return;
                Logger.LogDebug("Getting files for emulator {0}", emu.Title);
                //check if rom dir exists
                string romDir = emu.PathToRoms;
                if (string.IsNullOrEmpty(romDir) || !System.IO.Directory.Exists(romDir))
                {
                    Logger.LogWarn("Could not locate {0} rom directory '{1}'", emu.Title, romDir);
                    continue;
                }

                //get list of files using each filter
                foreach (string filter in emu.Filter.Split(';'))
                {
                    if (!doWork) return;
                    string[] gamePaths;
                    try { gamePaths = System.IO.Directory.GetFiles(romDir, filter, System.IO.SearchOption.AllDirectories); }
                    catch
                    {
                        Logger.LogError("Error locating files in {0} rom directory using filter '{1}'", emu.Title, filter);
                        continue; //error with filter, skip
                    }

                    //loop through each new file
                    for (int x = 0; x < gamePaths.Length; x++)
                    {
                        if (!doWork) return;
                        string path = gamePaths[x];
                        //check that path is not ignored, already in DB or not already picked up by a previous filter
                        if (!EmulatorsCore.Options.ShouldIgnoreFile(path) && !dbPaths.Contains(path))
                        {
                            filesFound++;
                            OnProgressChanged(new ImportProgressEventArgs(0, 0, filesFound, string.Format("Updating {0}", emu.Title)));
                            dbPaths.Add(path);
                            newGames.Add(new Game(emu, path));
                        }
                    }
                }
            }
            Logger.LogDebug("Found {0} new game(s)", filesFound);
            if (filesFound < 1)
                return;

            int filesAdded = 0;
            EmulatorsCore.Database.BeginTransaction();
            foreach (Game game in newGames)
            {
                filesAdded++;
                Logger.LogDebug("Commiting " + game.Title);
                OnProgressChanged(new ImportProgressEventArgs((filesAdded * 100) / filesFound, filesAdded, filesFound, "Commiting " + game.Title));
                game.Commit();
            }
            EmulatorsCore.Database.EndTransaction();
            OnImportStatusChanged(new ImportStatusEventArgs(ImportAction.NewFilesFound, null));
        }

        void deleteMissingGames(List<Game> games)
        {
            OnProgressChanged(new ImportProgressEventArgs(0, 0, 0, "Removing deleted games"));
            List<string> missingDrives = new List<string>();
            EmulatorsCore.Database.BeginTransaction();
            foreach (Game game in games)
            {
                List<GameDisc> missingDiscs = new List<GameDisc>();
                foreach (GameDisc disc in game.Discs)
                {
                    string path = disc.Path;
                    string drive = Path.GetPathRoot(path);
                    if (!missingDrives.Contains(drive))
                    {
                        //if path root is missing assume file is on disconnected
                        //removable/network drive and don't delete
                        if (!Directory.Exists(drive))
                            missingDrives.Add(drive);
                        else if (!File.Exists(path))
                            missingDiscs.Add(disc);
                    }
                }

                if (missingDiscs.Count == game.Discs.Count)
                {
                    Logger.LogDebug("Removing {0} from the database, file not found", game.Title);
                    OnProgressChanged(new ImportProgressEventArgs(0, 0, 0, string.Format("Removing {0}", game.Title)));
                    game.Delete();
                }
                else if (missingDiscs.Count > 0)
                {
                    foreach (GameDisc disc in missingDiscs)
                    {
                        Logger.LogDebug("Removing disc {0}, file not found", disc.Path);
                        OnProgressChanged(new ImportProgressEventArgs(0, 0, 0, string.Format("Removing disc {0}", disc.Path)));
                        game.Discs.Remove(disc);
                        disc.Delete();
                    }
                    Logger.LogDebug("Updating {0}, disc not found", game.Title);
                    game.Discs.Commit();
                }
                if (!doWork) break;
            }
            EmulatorsCore.Database.EndTransaction();
        }

        void setListCapacities(int capacity)
        {
            lock (pendingMatches)
                pendingMatches.Capacity = capacity;
            lock (priorityPendingMatches)
                priorityPendingMatches.Capacity = capacity;
            lock (approvedMatches)
                approvedMatches.Capacity = capacity;
            lock (priorityApprovedMatches)
                priorityApprovedMatches.Capacity = capacity;
            lock (matchesNeedingInput)
                matchesNeedingInput.Capacity = capacity;
            lock (commitedMatches)
                commitedMatches.Capacity = capacity;
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

            scanProgress("Retrieving matches: " + romMatch.Path);

            //get possible matches and
            //update RomMatch
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

            //get info
            ScraperGame scraperGame = scraperProvider.DownloadInfo(selectedMatch); //selectedScraper.DownloadInfo(selectedMatch);
            if (!doWork || !romMatch.OwnedByThread())
                return;

            ThumbGroup thumbGroup = new ThumbGroup(romMatch.Game);

            using (Bitmap image = getImage(scraperGame.BoxFrontUrl, romMatch))
            {
                if (!doWork)
                    return;
                thumbGroup.FrontCover.Image = image;
            }
            lock (romMatch.SyncRoot)
            {
                if (!romMatch.OwnedByThread())
                    return;
                thumbGroup.SaveThumb(ThumbType.FrontCover);
                thumbGroup.FrontCover.Image = null;
            }

            using (Bitmap image = getImage(scraperGame.BoxBackUrl, romMatch))
            {
                if (!doWork)
                    return;
                thumbGroup.BackCover.Image = image;
            }
            lock (romMatch.SyncRoot)
            {
                if (!romMatch.OwnedByThread())
                    return;
                thumbGroup.SaveThumb(ThumbType.BackCover);
                thumbGroup.BackCover.Image = null;
            }

            using (Bitmap image = getImage(scraperGame.TitleScreenUrl, romMatch))
            {
                if (!doWork)
                    return;
                thumbGroup.TitleScreen.Image = image;
            }
            lock (romMatch.SyncRoot)
            {
                if (!romMatch.OwnedByThread())
                    return;
                thumbGroup.SaveThumb(ThumbType.TitleScreen);
                thumbGroup.TitleScreen.Image = null;
            }

            using (Bitmap image = getImage(scraperGame.InGameUrl, romMatch))
            {
                if (!doWork)
                    return;
                thumbGroup.InGame.Image = image;
            }
            lock (romMatch.SyncRoot)
            {
                if (!romMatch.OwnedByThread())
                    return;
                thumbGroup.SaveThumb(ThumbType.InGameScreen);
                thumbGroup.InGame.Image = null;
            }

            using (Bitmap image = getImage(scraperGame.FanartUrl, romMatch))
            {
                if (!doWork)
                    return;
                thumbGroup.Fanart.Image = image;
            }
            lock (romMatch.SyncRoot)
            {
                if (!romMatch.OwnedByThread())
                    return;
                thumbGroup.SaveThumb(ThumbType.Fanart);
                thumbGroup.Fanart.Image = null;
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

        Bitmap getImage(string url, RomMatch romMatch)
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
            return result.Bitmap;
        }

        //update and commit game
        void commitGame(RomMatch romMatch)
        {
            Game dbGame = EmulatorsCore.Database.Get<Game>(romMatch.ID);
            if (dbGame == null)
                return; //game deleted

            ScraperGame details = romMatch.ScraperGame;
            if (details == null || !doWork)
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
            {
                grade = grade / 10;
            }
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
            {
                if (pendingMatches.Contains(match))
                    pendingMatches.Remove(match);
            }

            lock (priorityPendingMatches)
            {
                if (priorityPendingMatches.Contains(match))
                {
                    priorityPendingMatches.Remove(match);
                }
            }

            lock (matchesNeedingInput)
            {
                if (matchesNeedingInput.Contains(match))
                    matchesNeedingInput.Remove(match);
            }

            lock (approvedMatches)
            {
                if (approvedMatches.Contains(match))
                    approvedMatches.Remove(match);
            }

            lock (priorityApprovedMatches)
            {
                if (priorityApprovedMatches.Contains(match))
                    priorityApprovedMatches.Remove(match);
            }

            lock (commitedMatches)
            {
                if (commitedMatches.Contains(match))
                {
                    commitedMatches.Remove(match);
                }
            }

            lock (lookupSync)
            {
                if (lookupMatch.ContainsKey(match.ID))
                {
                    lookupMatch.Remove(match.ID);
                }
            }
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
