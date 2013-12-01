using Emulators.Database;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Emulators
{
    public enum EmulatorType
    {
        Standard,
        PcGame,
        ManyEmulators
    }

    [DBTable("Emulators")]
    public class Emulator : ThumbItem, IComparable<Emulator>
    {
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

        public Emulator() { }
        public Emulator(EmulatorType specialRole)
        {
            switch (specialRole)
            {
                case EmulatorType.Standard:
                    Title = "New Emulator";
                    EmulatorProfiles.Add(new EmulatorProfile(true));
                    break;
                case EmulatorType.PcGame:
                    Id = -1;
                    Title = "PC";
                    Platform = "Windows";
                    break;
                case EmulatorType.ManyEmulators:
                    Id = -2;
                    break;
            }
        }

        EmulatorProfile defaultProfile = null;
        [DBField]
        public EmulatorProfile DefaultProfile
        {
            get
            {
                if (defaultProfile == null && EmulatorProfiles.Count > 0)
                    defaultProfile = EmulatorProfiles[0];
                return defaultProfile;
            }
            set
            {
                defaultProfile = value;
            }
        }

        [DBField]
        public override string Title 
        {
            get { return title; }
            set
            {
                title = value;
                CommitNeeded = true;
            }
        }
        string title = "";

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
        
        public void SetCaseAspect(string aspect)
        {
            int index = aspect.TrimStart().IndexOf(" ");
            if (index > -1)
                aspect = aspect.Substring(0, index);

            double caseAspect;
            if (double.TryParse(aspect, out caseAspect))
                CaseAspect = caseAspect;
        }

        public bool IsPc()
        {
            return Id == -1;
        }

        public bool IsManyEmulators()
        {
            return Id == -2;
        }

        public override string ToString()
        {
            return Title;
        }

        DBRelationList<EmulatorProfile> profiles = null;
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

        object gamesSync = new object();
        bool gamesModified = true;
        List<Game> dbGames = null;
        List<Game> games = null;
        public List<Game> Games
        {
            get
            {
                lock (gamesSync)
                {
                    if (gamesModified)
                    {
                        if (dbGames == null)
                        {
                            DB.Instance.OnItemAdded += onGameAdded;
                            DB.Instance.OnItemDeleted += onGameDeleted;
                            BaseCriteria emuCriteria = new BaseCriteria(DBField.GetField(typeof(Game), "ParentEmulator"), "=", Id);
                            dbGames = DB.Instance.Get<Game>(emuCriteria);
                        }
                        games = new List<Game>(dbGames);
                        gamesModified = false;
                    }
                    return games;
                }
            }
        }

        void onGameAdded(DBItem changedItem)
        {
            Game changedGame = changedItem as Game;
            if (changedGame != null && changedGame.ParentEmulator == this)
            {
                lock (gamesSync)
                {
                    if (!dbGames.Contains(changedGame))
                    {
                        dbGames.Add(changedGame);
                        gamesModified = true;
                    }
                }
            }
        }

        void onGameDeleted(DBItem changedItem)
        {
            Game changedGame = changedItem as Game;
            if (changedGame != null && changedGame.ParentEmulator == this)
            {
                lock (gamesSync)
                {
                    dbGames.Remove(changedGame);
                    gamesModified = true;
                }
            }
        }

        public override void BeforeDelete()
        {
            DeleteThumbs();
            DB.Instance.BeginTransaction();
            foreach (Game game in Games)
                game.Delete();
            foreach (EmulatorProfile profile in EmulatorProfiles)
                profile.Delete();
            DB.Instance.EndTransaction();
            base.BeforeDelete();
        }

        public static Emulator GetPC()
        {
            return DB.Instance.Get<Emulator>(-1);
        }
        
        public void SavePosition()
        {
            Commit();
        }

        public int CompareTo(Emulator other)
        {
            if (other == null)
                return 1;
            return this.position.CompareTo(other.position);
        }

        public override string ThumbFolder
        {
            get { return ThumbGroup.EMULATOR_DIR_NAME; }
        }
    }
}
