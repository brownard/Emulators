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

    //Scans the file system and attempts to retrieve details for any new items.
    //The importer is based on code from MovingPictures. Credits to the MovingPictures team!
    public class Importer
    {
        //CommunityServerWCFServiceClient client = null;
        //DateTime lastConnectionErrorTime = new DateTime();
        int threadCount = 5;
        int hashThreadCount = 2;
        /// <summary>
        /// used to synchronise Start() and Stop()
        /// </summary>
        readonly object syncRoot = new object();
        volatile bool doWork = false;
        /// <summary>
        /// Used to store a complete list of roms currently importing
        /// </summary>
        Dictionary<int, RomMatch> lookupMatch;
        readonly object lookupSync = new object();

        List<Thread> importerThreads;
        volatile int percentDone = 0;
        volatile bool pause = false;

        #region Constructors

        public Importer()
        {
            init();
        }

        /// <summary>
        /// Creates a new Importer with the specified background state.
        /// </summary>
        /// <param name="isBackground">
        /// If true the importer will start downloading data immediately, else the importer will
        /// return a list of files to import and await a call to StartRetrieving()
        /// </param>
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
            int processed = lookupMatch.Count - pendingMatches.Count - priorityPendingMatches.Count - pendingServer.Count - priorityPendingServer.Count - pendingHashes.Count - priorityPendingHashes.Count;
            int total = lookupMatch.Count;
            OnProgressChanged(new ImportProgressEventArgs(percentDone, processed, total, message));
        }

        private void retrieveProgress(string message)
        {
            UpdatePercentDone();
            int total = lookupMatch.Count - matchesNeedingInput.Count;
            int processed = total - approvedMatches.Count - priorityApprovedMatches.Count - pendingMatches.Count - priorityPendingMatches.Count - pendingServer.Count - priorityPendingServer.Count - pendingHashes.Count - priorityPendingHashes.Count;
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
            int mediaScanned = lookupMatch.Count - pendingMatches.Count - priorityPendingMatches.Count - pendingServer.Count - priorityPendingServer.Count - pendingHashes.Count - priorityPendingHashes.Count;
            int mediaCommitted = commitedMatches.Count;

            percentDone = (int)Math.Round(((double)mediaScanned + mediaCommitted) * 100 / ((double)totalActions));

            if (percentDone > 100)
                percentDone = 100;
        }

        #endregion

        #region Public Properties

        bool isBackground = false;
        bool justRefresh = false;
        public bool JustRefresh
        {
            get { return justRefresh; }
            set { justRefresh = value; }
        }

        ScraperProvider scraperProvider = null;
        internal ScraperProvider ScraperProvider
        {
            get { return scraperProvider; }
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

        // Matches that have not yet been hashed.
        ReadOnlyCollection<RomMatch> PendingHashes
        {
            get { return pendingHashes.AsReadOnly(); }
        } private List<RomMatch> pendingHashes;

        // Matches that have not yet been checked with the community server.
        public ReadOnlyCollection<RomMatch> PendingServer
        {
            get { return pendingServer.AsReadOnly(); }
        } private List<RomMatch> pendingServer;

        // Matches that have not yet been scraped.
        public ReadOnlyCollection<RomMatch> PendingMatches
        {
            get { return pendingMatches.AsReadOnly(); }
        } private List<RomMatch> pendingMatches;

        // Same as PendingHashes, but this list gets priority. Used for user based interaction.
        public ReadOnlyCollection<RomMatch> PriorityPendingHashes
        {
            get { return priorityPendingHashes.AsReadOnly(); }
        } private List<RomMatch> priorityPendingHashes;

        // Same as PendingServer, but this list gets priority. Used for user based interaction.
        public ReadOnlyCollection<RomMatch> PriorityPendingServer
        {
            get { return priorityPendingServer.AsReadOnly(); }
        } private List<RomMatch> priorityPendingServer;

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

        // Matches that the importer is currently pulling details for
        public ReadOnlyCollection<RomMatch> RetrievingDetailsMatches
        {
            get { return retrievingDetailsMatches.AsReadOnly(); }
        } private List<RomMatch> retrievingDetailsMatches;

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

                pendingHashes = new List<RomMatch>();
                pendingServer = new List<RomMatch>();
                pendingMatches = new List<RomMatch>();
                priorityPendingHashes = new List<RomMatch>();
                priorityPendingServer = new List<RomMatch>();
                priorityPendingMatches = new List<RomMatch>();
                matchesNeedingInput = new List<RomMatch>();
                approvedMatches = new List<RomMatch>();
                priorityApprovedMatches = new List<RomMatch>();
                retrievingDetailsMatches = new List<RomMatch>();
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

                //if (EmulatorsCore.Options.GetBoolOption("retrieveGameDetials") || EmulatorsCore.Options.GetBoolOption("submitGameDetails"))
                //{
                    //var myBinding = new BasicHttpBinding();
                    //var myEndpoint = new EndpointAddress("http://" + EmulatorsCore.Options.GetStringOption("communityServerAddress") + "/CommunityServerService/service");
                    //client = new CommunityServerWCFServiceClient(myBinding, myEndpoint);
                //}

                doWork = true;
                if (importerThreads.Count == 0)
                {
                    if (!justRefresh) //start retrieving immediately
                    {
                        ScanRomsDelegate scanMethod;
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
                    setRomStatus(romMatch, RomMatchStatus.PendingHash);
                    lock (priorityPendingHashes)
                        priorityPendingHashes.Add(romMatch);

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
                    romMatch.Status = RomMatchStatus.PendingHash;
                    romMatch.HighPriority = true;
                    romMatch.CurrentThreadId = null;
                    romMatch.GameDetails = null;
                    romMatch.PossibleGameDetails = new List<ScraperResult>();
                }
                lookupMatch[romMatch.ID] = romMatch;
            }
            romMatch.Game.InfoChecked = false;
            romMatch.Game.SaveInfoCheckedStatus();
            setRomStatus(romMatch, RomMatchStatus.PendingHash);
            lock (priorityPendingHashes)
                priorityPendingHashes.Add(romMatch);
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
        void scanRoms(ScanRomsDelegate scanMethod, bool raiseEvents)
        {
            try
            {
                bool processHashes = true;
                bool skippedHash = false;
                while (doWork)
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

                    int previousCommittedCount = commitedMatches.Count;
                    int previousMatchesCount = lookupMatch.Count;
                    // if there is nothing to process, then sleep
                    while (pendingHashes.Count == 0 &&
                           pendingServer.Count == 0 &&
                           pendingMatches.Count == 0 &&
                           approvedMatches.Count == 0 &&
                           priorityPendingMatches.Count == 0 &&
                           priorityApprovedMatches.Count == 0 &&
                           priorityPendingHashes.Count == 0 &&
                           priorityPendingServer.Count == 0 &&
                           commitedMatches.Count == previousCommittedCount &&
                           previousMatchesCount == lookupMatch.Count &&
                           doWork
                           )
                    {
                        if (!checkAndWait(100, 10, false))
                            return;
                    }

                    if (skippedHash)
                    {
                        processHashes = true;
                        skippedHash = false;
                    }
                    if (!processHashes)
                        skippedHash = true;

                    scanMethod.Invoke(ref processHashes);

                    if (!doWork)
                        return;

                    UpdatePercentDone();
                    lock (importStatusLock)
                    {
                        if (!doWork)
                            return;

                        // if we are now just waiting on the user, say so
                        if (pendingHashes.Count == 0 && priorityPendingHashes.Count == 0 &&
                            pendingServer.Count == 0 && priorityPendingServer.Count == 0 &&
                            pendingMatches.Count == 0 && approvedMatches.Count == 0 &&
                            priorityPendingMatches.Count == 0 && priorityApprovedMatches.Count == 0 &&
                            lookupMatch.Count == commitedMatches.Count + matchesNeedingInput.Count &&
                            matchesNeedingInput.Count > 0)
                        {
                            OnProgressChanged(new ImportProgressEventArgs(percentDone, 0, matchesNeedingInput.Count, "Waiting for Approvals..."));
                            if (isBackground)
                            {
                                doWork = false;
                                ImporterStatus = ImportAction.ImportFinished;
                                return;
                            }
                        }

                        // if we are now just waiting on the user, say so
                        if (pendingHashes.Count == 0 && priorityPendingHashes.Count == 0 &&
                            pendingServer.Count == 0 && priorityPendingServer.Count == 0 &&
                            pendingMatches.Count == 0 && approvedMatches.Count == 0 &&
                            priorityPendingMatches.Count == 0 && priorityApprovedMatches.Count == 0 &&
                            matchesNeedingInput.Count == 0)
                        {
                            if (commitedMatches.Count == lookupMatch.Count)
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
            }
            catch (Exception e)
            {
                Logger.LogError("Unhandled error in Importer - {0} - {1}\r\n{2}", e, e.Message, e.StackTrace);
            }
        }

        delegate void ScanRomsDelegate(ref bool processHashes);
        // The order is to have the user selected roms be done first, and then the standard import order second.
        // The order needs to be Hash, Server, Pending Matches, Approved Matches
        void scanRomsDefault(ref bool processHashes)
        {
            if (processHashes && priorityPendingHashes.Count > 0)
                processHashes = processNextPendingHash(true);
            else if (priorityPendingServer.Count > 0)
                processNextPendingServer(true);
            else if (priorityPendingMatches.Count > 0)
                processNextPendingMatch(true);
            else if (priorityApprovedMatches.Count > 0)
                processNextApprovedMatch(true);
            else if (processHashes && pendingHashes.Count > 0)
                processHashes = processNextPendingHash(false);
            else if (pendingServer.Count > 0)
                processNextPendingServer(false);
            else if (pendingMatches.Count > 0)
                processNextPendingMatch(false);
            else if (approvedMatches.Count > 0)
                processNextApprovedMatch(false);
        }

        //alternate order when background, get info asap so GUI can be updated
        void scanRomsBackground(ref bool processHashes)
        {
            if (priorityApprovedMatches.Count > 0)
                processNextApprovedMatch(true);
            else if (priorityPendingMatches.Count > 0)
                processNextPendingMatch(true);
            else if (priorityPendingServer.Count > 0)
                processNextPendingServer(true);
            else if (processHashes && priorityPendingHashes.Count > 0)
                processHashes = processNextPendingHash(true);
            else if (approvedMatches.Count > 0)
                processNextApprovedMatch(false);
            else if (pendingMatches.Count > 0)
                processNextPendingMatch(false);
            else if (pendingServer.Count > 0)
                processNextPendingServer(false);
            else if (processHashes && pendingHashes.Count > 0)
                processHashes = processNextPendingHash(false);
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
                                lock (pendingHashes)
                                {
                                    pendingHashes.Add(romMatch);
                                }
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
            lock (pendingHashes)
                pendingHashes.Capacity = capacity;
            lock (priorityPendingHashes)
                priorityPendingHashes.Capacity = capacity;
            lock (pendingServer)
                pendingServer.Capacity = capacity;
            lock (priorityPendingServer)
                priorityPendingServer.Capacity = capacity;
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

        #region Hash
        //object hashLock = new object();
        //int currentHashThreads = 0;

        /// <summary>
        /// CURRENTLY DISABLED UNTIL COMMUNITY SERVER READY
        /// Grabs the next game in the list and hashes it.
        /// </summary>
        private bool processNextPendingHash(bool priorityOnly)
        {
            //lock (hashLock)
            //{
            //    if (currentHashThreads >= hashThreadCount)
            //        return false;
            //    currentHashThreads++;
            //}

            RomMatch romMatch = takeFromList(pendingHashes, priorityPendingHashes, priorityOnly);
            if (romMatch == null)
                return true;

            //try
            //{
            //    Hasher.Hasher.Hashes hashes;
            //    if (romMatch.Game.Hash.Trim().Length == 0)
            //    {
            //        hashes = Hasher.Hasher.CalculateHashes(romMatch.Game.Path, delegate(string fileName, int percentComplete)
            //        {
            //            if (romMatch.Ignore || !doWork)
            //                return 0;
            //            else
            //                return OnHashProgress(fileName, percentComplete);
            //        });
            //    }
            //    else
            //    {
            //        hashes = new Hasher.Hasher.Hashes();
            //        hashes.ed2k = romMatch.Game.Hash;
            //        hashes.crc32 = "";
            //        hashes.md5 = "";
            //        hashes.sha1 = "";
            //    }

            //    // Put hash into game object
            //    romMatch.Game.Hash = hashes.ed2k;

            //}
            //catch (Exception ex)
            //{
            //    Logger.LogError("Error hashing {0} - {1}", romMatch.Game.Title, ex.Message);
            //    Logger.LogInfo("Importer: Ignoring {0}", romMatch.Game.Title);
            //    Ignore(romMatch);

            //    lock (hashLock)
            //        currentHashThreads--;

            //    return true;
            //}

            //lock (hashLock)
            //    currentHashThreads--;


            //// Pass game onto Community server queue
            //if (romMatch.HighPriority)
            //{
            //    lock (priorityPendingServer.SyncRoot)
            //    {
            //        if (!romMatch.Ignore)
            //            priorityPendingServer.Add(romMatch);
            //    }
            //}
            //else
            //{
            //    lock (pendingServer.SyncRoot)
            //    {
            //        if (!romMatch.Ignore)
            //            pendingServer.Add(romMatch);
            //    }
            //}

            //skip straight to pending matches until community server finishes
            addToList(romMatch, RomMatchStatus.PendingMatch, pendingMatches, priorityPendingMatches);
            return true;
        }

        /// <summary>
        /// Receives current percentage of the hash progress of the specific file
        /// </summary>
        /// <param name="fileName">Path of file being hashed</param>
        /// <param name="percentComplete">Percentage of hash progress completed</param>
        /// <returns></returns>
        public int OnHashProgress(string fileName, int percentComplete)
        {
            UpdatePercentDone();
            int processed = lookupMatch.Count - pendingHashes.Count - priorityPendingHashes.Count;
            int total = lookupMatch.Count;
            OnProgressChanged(new ImportProgressEventArgs(percentDone, processed, total, "Hashing File: " + fileName + " - " + percentComplete + "%"));
            if (!doWork)
                return 0;
            return 1; //continue hashing (return 0 to abort)
        }
        #endregion

        #region Server
        private void processNextPendingServer(bool priorityOnly)
        {
            //RomMatch romMatch = takeFromList(pendingServer, priorityPendingServer, priorityOnly);
            //if (romMatch == null)
            //    return;

            //// Get game info from community server.
            //TimeSpan checkingTime = new TimeSpan(0, EmulatorsCore.Options.GetIntOption("communityServerConnectionRetryTime"), 0);
            //DateTime checkTime = DateTime.Now.Subtract(checkingTime);

            //myemulators2.v1.GameDetails receivedGameDetails = null;

            //if (lastConnectionErrorTime > checkTime && EmulatorsCore.Options.GetBoolOption("retrieveGameDetials"))
            //{
            //    try
            //    {
            //        myemulators2.v1.GameDetails sendGameDetails = new myemulators2.v1.GameDetails();

            //        sendGameDetails.Hash = romMatch.Game.Hash;
            //        sendGameDetails.Filename = romMatch.Path;

            //        receivedGameDetails = client.RequestGameDetials(sendGameDetails);

            //        Logger.LogDebug("{0}", client.State.ToString());
            //    }
            //    catch (TimeoutException timeout)
            //    {
            //        // Handle the timeout exception.
            //        Logger.LogError("Community Server Timeout Error: " + timeout.Message);
            //        lastConnectionErrorTime = DateTime.Now;
            //        client.Abort();
            //    }
            //    catch (CommunicationException commException)
            //    {
            //        // Handle the communication exception.
            //        Logger.LogError("Community Server Error: " + commException.Message);
            //        lastConnectionErrorTime = DateTime.Now;
            //        client.Abort();
            //    }
            //}

            //// Pass game onto meding matches queue
            //addToList(romMatch, RomMatchStatus.PendingMatch, pendingMatches, priorityPendingMatches);
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
            lock (pendingHashes)
            {
                if (pendingHashes.Contains(match))
                    pendingHashes.Remove(match);
            }

            lock (priorityPendingHashes)
            {
                if (priorityPendingHashes.Contains(match))
                {
                    priorityPendingHashes.Remove(match);
                }
            }

            lock (pendingServer)
            {
                if (pendingServer.Contains(match))
                    pendingServer.Remove(match);
            }

            lock (priorityPendingServer)
            {
                if (priorityPendingServer.Contains(match))
                {
                    priorityPendingServer.Remove(match);
                }
            }

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

            lock (retrievingDetailsMatches)
            {
                if (retrievingDetailsMatches.Contains(match))
                {
                    retrievingDetailsMatches.Remove(match);
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
