﻿using Emulators.Import;
using Emulators.Scrapers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Emulators
{
    public class RomMatch
    {
        static Regex romCodeRegEx = new Regex(@"[(\[].*?[)\]]");
        static Regex theAppendRegEx = new Regex(@", the\b", RegexOptions.IgnoreCase);
        public RomMatch(Game game)
        {
            if (game != null)
            {
                this.game = game;
                id = game.Id.Value;
                Path = game.CurrentDisc.Path;
                Filename = game.CurrentDisc.Filename;
                searchTitle = string.IsNullOrEmpty(game.SearchTitle) ? game.Title : game.SearchTitle;
                IsDefaultSearchTerm = game.GetDefaultTitle() == searchTitle;
            }
            PossibleGameDetails = new List<ScraperResult>();
            ResetDisplayInfo();
        }

        Game game = null;
        public Game Game
        {
            get { return game; }
        }

        object syncRoot = new object();
        public object SyncRoot { get { return syncRoot; } }

        public ScraperResult GameDetails
        {
            get;
            set;
        }

        public List<ScraperResult> PossibleGameDetails
        {
            get;
            set;
        }

        public ScraperGame ScraperGame
        {
            get;
            set;
        }

        string searchTitle;
        public string SearchTitle
        {
            get { return searchTitle; }
            set
            {
                searchTitle = value;
                IsDefaultSearchTerm = searchTitle == Filename;
            }
        }

        public string Path
        {
            get;
            private set;
        }

        public string Filename
        {
            get;
            private set;
        }

        public bool IsDefaultSearchTerm
        {
            get;
            private set;
        }

        bool isMultiFile;
        public bool IsMultiFile
        {
            get
            {
                return isMultiFile;
            }
        }

        string displayFilename;
        public string DisplayFilename
        {
            get
            {
                return displayFilename;
            }
        }

        string toolTip;
        public string ToolTip 
        { 
            get 
            {
                return toolTip;
            } 
        }

        public void ResetDisplayInfo()
        {
            if (game != null)
            {
                List<GameDisc> discs = game.Discs;
                isMultiFile = discs.Count > 1;
                displayFilename = isMultiFile ? "Multiple files" : Filename;
                toolTip = "";
                for (int x = 0; x < discs.Count; x++)
                {
                    if (x > 0)
                        toolTip += ",\r\n";
                    toolTip += discs[x].Path;
                }
            }
        }

        int id = -1;
        public int ID
        {
            get { return id; }
            set { id = value; }
        }

        public bool HighPriority
        {
            get;
            set;
        }
        
        RomMatchStatus status = RomMatchStatus.PendingMatch;
        public RomMatchStatus Status
        {
            get { return status; }
            set { status = value; }
        }

        public int? CurrentThreadId
        {
            get;
            set;
        }

        public bool OwnedByThread()
        {
            bool result;
            lock (syncRoot)
                result = CurrentThreadId == System.Threading.Thread.CurrentThread.ManagedThreadId;
            return result;
        }

        public int BindingSourceIndex { get; set; }
    }

    public enum RomMatchStatus
    {
        PendingMatch = 1,
        Approved = 2,
        Committed = 3,
        NeedsInput = 4,
        Removed = 5,
        Ignored = 6
    }
}
