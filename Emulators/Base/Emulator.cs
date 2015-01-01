using Emulators.Database;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Emulators
{
    [DBTable("Emulators")]
    public class Emulator : ThumbItem, IComparable<Emulator>
    {
        #region Get Emulators

        /// <summary>
        /// Retrieves all emulators from the database.
        /// </summary>
        /// <param name="hidePcIfEmpty">Whether to not return the PC emulator if no PC games have been configured</param>
        public static List<Emulator> GetAll(bool hidePcIfEmpty = false)
        {
            List<Emulator> result = DB.Instance.GetAll<Emulator>();
            if (hidePcIfEmpty)
            {
                Emulator pc = Emulator.GetPC();
                if (pc != null && pc.Games.Count < 1)
                    result.Remove(pc);
            }
            return result;
        }

        /// <summary>
        /// Returns the PC games emulator
        /// </summary>
        public static Emulator GetPC()
        {
            return DB.Instance.Get<Emulator>(-1);
        }

        #endregion

        #region Ctor

        /// <summary>
        /// Creates a new, blank emulator
        /// </summary>
        public static Emulator CreateNewEmulator()
        {
            Emulator emu = new Emulator() { Title = "New Emulator" };
            emu.EmulatorProfiles.Add(new EmulatorProfile(true));
            return emu;
        }

        /// <summary>
        /// Creates a new PC emulator. Called on DB init if a PC emulator doesn't currently exist
        /// </summary>
        internal static Emulator CreateNewPCEmulator()
        {
            return new Emulator()
            {
                Id = -1,
                Title = "PC",
                Platform = "Windows"
            };
        }

        public Emulator() { }

        #endregion

        #region Properties

        /// <summary>
        /// The display name of the emulator
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
        /// The platform/console that this emulator emulates
        /// </summary>
        [DBField]
        public string Platform
        {
            get { return platform; }
            set
            {
                platform = value;
                CommitNeeded = true;
            }
        }
        string platform = "";

        /// <summary>
        /// Th developer/company that created the emulated console
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
        /// The year that the console was released
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
        /// Description of the console
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
        /// The user rating of the console
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
        /// The list position of the emulator
        /// </summary>
        [DBField]
        public int Position
        {
            get { return position; }
            set
            {
                position = value;
                CommitNeeded = true;
            }
        }
        int position = 0;

        /// <summary>
        /// Path to the directory containing the roms
        /// </summary>
        [DBField]
        public string PathToRoms
        {
            get { return pathToRoms; }
            set
            {
                pathToRoms = value;
                CommitNeeded = true;
            }
        }
        string pathToRoms = "";

        /// <summary>
        /// The file filter used when searching the rom directory
        /// </summary>
        [DBField]
        public string Filter
        {
            get { return filter; }
            set
            {
                filter = value;
                CommitNeeded = true;
            }
        }
        string filter = "";

        /// <summary>
        /// The last view used to display the roms
        /// </summary>
        [DBField]
        public int View
        {
            get { return view; }
            set
            {
                view = value;
                CommitNeeded = true;
            }
        }
        int view = 0;

        /// <summary>
        /// The aspect ratio of the console's game case. Used to resize cover art to a consistent size
        /// </summary>
        [DBField]
        public double CaseAspect
        {
            get { return caseAspect; }
            set
            {
                caseAspect = value;
                CommitNeeded = true;
            }
        }
        double caseAspect = 0;

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

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the case aspect from a decimal as a string
        /// </summary>
        /// <param name="aspect"></param>
        public void SetCaseAspect(string aspect)
        {
            int index = aspect.TrimStart().IndexOf(" ");
            if (index > -1)
                aspect = aspect.Substring(0, index);

            double caseAspect;
            if (double.TryParse(aspect, out caseAspect))
                CaseAspect = caseAspect;
        }

        /// <summary>
        /// Saves the emulator's list position. Currently just does a full commit.
        /// </summary>
        public void SavePosition()
        {
            Commit();
        }

        /// <summary>
        /// Whether this emulator plays PC games
        /// </summary>
        /// <returns></returns>
        public bool IsPc()
        {
            return Id == -1;
        }

        public override string ToString()
        {
            return Title;
        }

        #endregion

        #region Profiles

        EmulatorProfile defaultProfile = null;
        /// <summary>
        /// The default profile to use for all games that haven't specified a specific profile
        /// </summary>
        [DBField]
        public EmulatorProfile DefaultProfile
        {
            get
            {
                //default to the first profile if default profile not set
                if (defaultProfile == null && EmulatorProfiles.Count > 0)
                    defaultProfile = EmulatorProfiles[0];
                return defaultProfile;
            }
            set
            {
                defaultProfile = value;
            }
        }

        DBRelationList<EmulatorProfile> profiles = null;
        /// <summary>
        /// All configured profiles for this emulator
        /// </summary>
        [DBRelation(AutoRetrieve = true)]
        public DBRelationList<EmulatorProfile> EmulatorProfiles
        {
            get
            {
                if (profiles == null)
                    profiles = new DBRelationList<EmulatorProfile>(this);
                return profiles;
            }
        }

        #endregion

        #region Games

        object gamesSync = new object();
        List<Game> dbGames = null;
        ReadOnlyCollection<Game> games = null;
        /// <summary>
        /// Lazily retrieves the games associated with this emulator
        /// </summary>
        public ReadOnlyCollection<Game> Games
        {
            get
            {
                //see if we can avoid getting a lock
                ReadOnlyCollection<Game> lGames = games;
                if (lGames != null)
                    return lGames;

                lock (gamesSync)
                {
                    if (games == null)
                    {
                        if (dbGames == null)
                        {
                            //inital init of backing list
                            DB.Instance.OnItemAdded += onGameAdded;
                            DB.Instance.OnItemDeleted += onGameDeleted;
                            BaseCriteria emuCriteria = new BaseCriteria(DBField.GetField(typeof(Game), "ParentEmulator"), "=", Id);
                            dbGames = DB.Instance.Get<Game>(emuCriteria);
                        }
                        //create a seperate, raad only collection that won't be modified on database changes
                        games = new List<Game>(dbGames).AsReadOnly();
                    }
                    return games;
                }
            }
        }

        /// <summary>
        /// FIred when an item has been added to the database
        /// </summary>
        void onGameAdded(DBItem changedItem)
        {
            Game changedGame = changedItem as Game;
            //see if item is a game and belongs to this emulator
            if (changedGame != null && changedGame.ParentEmulator == this)
            {
                lock (gamesSync)
                {
                    if (!dbGames.Contains(changedGame))
                    {
                        //update backing list and invalidate front list
                        dbGames.Add(changedGame);
                        games = null;
                    }
                }
            }
        }

        /// <summary>
        /// FIred when an item has been deleted from the database
        /// </summary>
        void onGameDeleted(DBItem changedItem)
        {
            Game changedGame = changedItem as Game;
            //see if item is a game and belongs to this emulator
            if (changedGame != null && changedGame.ParentEmulator == this)
            {
                lock (gamesSync)
                {
                    //update backing list and invalidate front list
                    dbGames.Remove(changedGame);
                    games = null;
                }
            }
        }

        #endregion

        #region DBItem Overrides

        public override void BeforeDelete()
        {
            //delete associated thumbs
            DeleteThumbs();
            DB.Instance.BeginTransaction();
            //delete all games
            foreach (Game game in Games)
                game.Delete();
            //delete all profiles
            foreach (EmulatorProfile profile in EmulatorProfiles)
                profile.Delete();
            DB.Instance.EndTransaction();
            base.BeforeDelete();
        }

        #endregion

        #region ThumbItem Overrides

        public override string ThumbFolder
        {
            get { return ThumbGroup.EMULATOR_DIR_NAME; }
        }

        #endregion

        #region IComparable

        /// <summary>
        /// Compares the list positions
        /// </summary>
        public int CompareTo(Emulator other)
        {
            if (other == null)
                return 1;
            return this.position.CompareTo(other.position);
        }

        #endregion
    }
}
