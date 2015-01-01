using Emulators.Database;
using Emulators.Import;
using System;
using System.Collections.Generic;
using System.Text;

namespace Emulators
{
    [DBTable("Games")]
    public class Game : ThumbItem, IComparable<Game>
    {
        #region Get Games

        /// <summary>
        /// Retrieves all games from the database
        /// </summary>
        public static List<Game> GetAll()
        {
            return DB.Instance.GetAll<Game>();
        }

        /// <summary>
        /// Retrieves all games that have/haven't been imported
        /// </summary>
        /// <param name="haveBeenImported">Whether to return games that have/haven't been imported</param>
        public static List<Game> GetAll(bool haveBeenImported)
        {
            return DB.Instance.Get<Game>(new BaseCriteria(DBField.GetField(typeof(Game), "InfoChecked"), "=", haveBeenImported));
        }

        #endregion

        #region Ctor

        public Game() { }

        /// <summary>
        /// Creates a new game for the specified emulator
        /// </summary>
        /// <param name="parentEmulator">The emulator that this game belongs to</param>
        /// <param name="path">The path to the game. This can be a path to a Goodmerge archive</param>
        /// <param name="launchFile">Optional file to extract if path is a Goodmerge archive</param>
        public Game(Emulator parentEmulator, string path, string launchFile = null)
        {
            this.parentEmulator = parentEmulator;
            if (parentEmulator.IsPc())
            {
                GameProfiles.Add(new EmulatorProfile(true) { SuspendMP = true });
                GameProfiles.Populated = true;
            }
            Discs.Add(new GameDisc(path, launchFile));
            Discs.Populated = true;
            Title = GetDefaultTitle();
        }

        #endregion

        #region Properties

        /// <summary>
        /// The emulator used to launch the game
        /// </summary>
        [DBField]
        public Emulator ParentEmulator
        {
            get { return parentEmulator; }
            set
            {
                parentEmulator = value;
                isGoodmerge = null;
                CommitNeeded = true;
            }
        }
        Emulator parentEmulator = null;

        /// <summary>
        /// The title of the game
        /// </summary>
        [DBField]
        public string Title
        {
            get { return title; }
            set
            {
                title = value;
                CommitNeeded = true;
            }
        }
        string title = "";

        /// <summary>
        /// The developer of the game
        /// </summary>
        [DBField]
        public string Developer
        {
            get { return developer; }
            set
            {
                developer = value;
                CommitNeeded = true;
            }
        }
        string developer = "";

        /// <summary>
        /// The release year of the game
        /// </summary>
        [DBField]
        public int Year
        {
            get { return year; }
            set
            {
                year = value;
                CommitNeeded = true;
            }
        }
        int year = 0;

        /// <summary>
        /// Description of the game
        /// </summary>
        [DBField]
        public string Description
        {
            get { return description; }
            set
            {
                description = value;
                CommitNeeded = true;
            }
        }
        string description = "";

        /// <summary>
        /// '|' delimited list of genres
        /// </summary>
        [DBField]
        public string Genre
        {
            get { return genre; }
            set
            {
                genre = value;
                CommitNeeded = true;
            }
        }
        string genre = "";

        /// <summary>
        /// The user rating of the game
        /// </summary>
        [DBField]
        public int Grade
        {
            get { return grade; }
            set
            {
                grade = value;
                CommitNeeded = true;
            }
        }
        int grade = 0;

        /// <summary>
        /// The number of times the game has been played
        /// </summary>
        [DBField]
        public int PlayCount
        {
            get { return playCount; }
            set
            {
                playCount = value;
                CommitNeeded = true;
            }
        }
        int playCount = 0;

        /// <summary>
        /// The date/time the game was last played
        /// </summary>
        [DBField]
        public DateTime Latestplay
        {
            get { return latestplay; }
            set
            {
                latestplay = value;
                CommitNeeded = true;
            }
        }
        DateTime latestplay = DateTime.MinValue;

        /// <summary>
        /// Whether the game has been marked as a favourite
        /// </summary>
        [DBField]
        public bool Favourite
        {
            get { return favourite; }
            set
            {
                favourite = value;
                CommitNeeded = true;
            }
        }
        bool favourite = false;

        /// <summary>
        /// Whether the game has been updated by the user/importer
        /// </summary>
        [DBField]
        public bool InfoChecked
        {
            get { return infoChecked; }
            set
            {
                infoChecked = value;
                CommitNeeded = true;
            }
        }
        bool infoChecked = false;

        /// <summary>
        /// Path to an associated video file
        /// </summary>
        [DBField]
        public string VideoPreview
        {
            get { return videoPreview; }
            set
            {
                if (string.IsNullOrEmpty(value))
                    videoPreview = "";
                else
                    videoPreview = value;
                CommitNeeded = true;
            }
        }
        string videoPreview = "";

        bool? isGoodmerge = null;
        /// <summary>
        /// Whether the currently selected disc is a Goodmerge archive
        /// </summary>
        public bool IsGoodmerge
        {
            get
            {
                if (isGoodmerge.HasValue)
                    return isGoodmerge.Value;

                isGoodmerge = false;
                EmulatorProfile profile = CurrentProfile;
                //check if we have a profile and it's configured to use goodmerge
                if (profile == null || !profile.EnableGoodmerge)
                    return false;

                string path = CurrentDisc.Path;
                if (string.IsNullOrEmpty(path))
                    return false;

                //get file extension
                string extension = System.IO.Path.GetExtension(path);
                if (string.IsNullOrEmpty(extension))
                    return false;

                //get configured Goodmerge extensions
                string[] goodmergeExts = Options.Instance.GetStringOption("goodmergefilters").Split(';');
                for (int x = 0; x < goodmergeExts.Length; x++)
                {
                    //see if extension matches filter
                    if (goodmergeExts[x].EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                    {
                        isGoodmerge = true;
                        return true;
                    }
                }
                //no match was found
                return false;
            }
        }

        /// <summary>
        /// Custom search term to use when retrieving online info
        /// </summary>
        public string SearchTitle { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Returns the default title for this game, based on the path or platform specific naming (e.g Mame)
        /// </summary>
        public string GetDefaultTitle()
        {
            string title = System.IO.Path.GetFileNameWithoutExtension(CurrentDisc.Path);
            if (parentEmulator != null && parentEmulator.Platform == "Arcade")
                title = MameNameHandler.Instance.GetName(title);
            return title;
        }

        /// <summary>
        /// Resets the game to a blank state, deleting all images and using the default title
        /// </summary>
        public void Reset()
        {
            this.Title = GetDefaultTitle();
            this.Grade = 0;
            this.Year = 0;
            this.Description = "";
            this.Genre = "";
            this.Developer = "";
            using (ThumbGroup thumbs = new ThumbGroup(this))
            {
                thumbs.FrontCover.Image = null;
                thumbs.BackCover.Image = null;
                thumbs.InGame.Image = null;
                thumbs.TitleScreen.Image = null;
                thumbs.Fanart.Image = null;
                thumbs.SaveAllThumbs();
            }
        }

        /// <summary>
        /// Determines whether the game has missing information
        /// </summary>
        /// <returns></returns>
        public bool IsMissingInfo()
        {
            if (this.Id == -2) return true;
            if (string.IsNullOrEmpty(this.Title)) return true;
            if (this.Grade == 0) return true;
            if (this.Year == 0) return true;
            if (string.IsNullOrEmpty(this.Description)) return true;
            if (string.IsNullOrEmpty(this.Genre)) return true;
            if (string.IsNullOrEmpty(this.Developer)) return true;
            //don't technically need to dispose of ThumbGroup as we don't load any images
            //but just in case
            using (ThumbGroup thumbs = new ThumbGroup(this))
            {
                if (string.IsNullOrEmpty(thumbs.FrontCover.Path))
                    return true;
                if (string.IsNullOrEmpty(thumbs.BackCover.Path))
                    return true;
                if (string.IsNullOrEmpty(thumbs.InGame.Path))
                    return true;
                if (string.IsNullOrEmpty(thumbs.TitleScreen.Path))
                    return true;
                if (string.IsNullOrEmpty(thumbs.Fanart.Path))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Saves the InfoChecked property. Currently just does a full commit
        /// </summary>
        internal void SaveInfoCheckedStatus()
        {
            Commit();
        }

        /// <summary>
        /// Saves the PlayCount and LatestPlay properties. Currently just does a full commit
        /// </summary>
        public void SaveGamePlayInfo()
        {
            Commit();
        }

        /// <summary>
        /// Increments PlayCount and sets LatestPlay to current date/time, then commits
        /// </summary>
        public void UpdateAndSaveGamePlayInfo()
        {
            playCount++;
            Latestplay = DateTime.Now;
            Commit();
        }

        public override string ToString()
        {
            return Title;
        }

        #endregion

        #region Discs

        GameDisc currentDisc = null;
        /// <summary>
        /// The currently selected disc
        /// </summary>
        [DBField]
        public GameDisc CurrentDisc
        {
            get
            {
                if (currentDisc == null && Discs.Count > 0)
                    currentDisc = Discs[0];
                return currentDisc;
            }
            set
            {
                currentDisc = value;
                isGoodmerge = null;
                CommitNeeded = true;
            }
        }

        DBRelationList<GameDisc> discs = null;
        /// <summary>
        /// All discs associated with this game
        /// </summary>
        [DBRelation(AutoRetrieve = true)]
        public DBRelationList<GameDisc> Discs
        {
            get
            {
                if (discs == null)
                    discs = new DBRelationList<GameDisc>(this);
                return discs;
            }
        }

        #endregion

        #region Profiles

        EmulatorProfile currentProfile = null;
        /// <summary>
        /// The currently selected profile used to launch this game
        /// </summary>
        [DBField]
        public EmulatorProfile CurrentProfile
        {
            get
            {
                if (currentProfile == null && EmulatorProfiles.Count > 0)
                    currentProfile = EmulatorProfiles[0];
                return currentProfile;
            }
            set
            {
                currentProfile = value;
                CommitNeeded = true;
            }
        }

        /// <summary>
        /// All profiles that can be used to launch this game
        /// </summary>
        public DBRelationList<EmulatorProfile> EmulatorProfiles
        {
            get
            {
                if (parentEmulator.IsPc())
                    return GameProfiles;
                return parentEmulator.EmulatorProfiles;
            }
        }

        DBRelationList<EmulatorProfile> profiles = null;
        /// <summary>
        /// If this game is a PC game, returns all profiles associated with this game
        /// </summary>
        [DBRelation(AutoRetrieve = true)]
        public DBRelationList<EmulatorProfile> GameProfiles
        {
            get
            {
                if (profiles == null)
                    profiles = new DBRelationList<EmulatorProfile>(this);
                return profiles;
            }
        }

        #endregion

        #region DBItem Overrides

        public override void BeforeDelete()
        {
            EmulatorsSettings.Instance.Importer.Remove(Id);
            DeleteThumbs();
            DB.Instance.BeginTransaction();
            foreach (GameDisc disc in Discs)
                disc.Delete();
            foreach (EmulatorProfile profile in GameProfiles)
                profile.Delete();
            DB.Instance.EndTransaction();
            base.BeforeDelete();
        }

        #endregion

        #region ThumbItem Overrides

        public override string ThumbFolder
        {
            get { return ThumbGroup.GAME_DIR_NAME; }
        }

        public override bool HasGameArt
        {
            get { return true; }
        }

        public override double AspectRatio
        {
            get
            {
                if (parentEmulator != null)
                    return parentEmulator.CaseAspect;
                return base.AspectRatio;
            }
        }

        public override ThumbItem DefaultThumbItem
        {
            get { return parentEmulator; }
        }

        #endregion

        #region IComparable

        public int CompareTo(Game other)
        {
            if (other == null)
                return 1;
            return this.title.CompareTo(other.title);
        }

        #endregion
    }
}
