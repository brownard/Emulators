using Emulators.Database;
using System;
using System.Collections.Generic;
using System.Text;

namespace Emulators
{
    [DBTable("Games")]
    public class Game : ThumbItem, IComparable<Game>
    {
        public static List<Game> GetAll(bool? imported = null)
        {
            if (imported.HasValue)
                return DB.Instance.Get<Game>(new BaseCriteria(DBField.GetField(typeof(Game), "InfoChecked"), "=", imported.Value));
            return DB.Instance.GetAll<Game>();
        }

        public Game() { }
        public Game(Emulator parentEmu, string path, string launchFile = null)
        {
            Title = titleFromPath(path);
            parentEmulator = parentEmu;
            if (parentEmulator.IsPc())
            {
                GameProfiles.Add(new EmulatorProfile(true) { SuspendMP = true });
                GameProfiles.Populated = true;
            }
            Discs.Add(new GameDisc(path, launchFile));
            Discs.Populated = true;            
        }

        private string titleFromPath(string path)
        {
            string s = "";
            int index = path.LastIndexOf(".");
            if (index > -1)
                s = path.Remove(index);

            if (s.Length > 0)
                s = s.Substring(s.LastIndexOf("\\") + 1);

            return s;
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

        private string description = "";
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

        private string genre = "";
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

        private int grade = 0;
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
        
        private int playCount = 0;
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

        private DateTime latestplay = DateTime.MinValue;
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

        [DBField]
        public string Arguments
        {
            get { return arguments; }
            set
            {
                arguments = value;
                CommitNeeded = true;
            }
        }
        string arguments = "";

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

        GameDisc currentDisc = null;
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

        EmulatorProfile currentProfile = null;
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
        
        public override string ToString()
        {
            return Title;
        }

        DBRelationList<GameDisc> discs = null;
        [DBRelation(AutoRetrieve = true)]
        public DBRelationList<GameDisc> Discs
        {
            get
            {
                if (discs == null)
                    discs = new DBRelationList<GameDisc>(this); //GetDiscs();
                return discs;
            }
        }

        //public List<EmulatorProfile> GetProfiles()
        //{
        //    if (!parentEmulator.IsPc())
        //        return parentEmulator.GetProfiles();

        //    BaseCriteria criteria = new BaseCriteria(DBField.GetField(typeof(EmulatorProfile), "ParentGame"), "=", Id);
        //    return DB.Instance.Get<EmulatorProfile>(criteria);
        //}

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

        /// <summary>
        /// Reset the game object to a blank state. Only the filepath and filename remains.
        /// </summary>
        public void Reset()
        {
            this.Title = titleFromPath(CurrentDisc.Path);
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
        
        public bool IsMissingInfo()
        {
            if (this.Id == -2) return true;
            if (String.IsNullOrEmpty(this.Title) ) return true;
            if (this.Grade == 0 ) return true;
            if (this.Year == 0) return true;
            if (String.IsNullOrEmpty(this.Description)) return true;
            if (String.IsNullOrEmpty(this.Genre)) return true;
            if (String.IsNullOrEmpty(this.Developer)) return true;
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

        bool? isGoodmerge = null;
        public bool IsGoodmerge
        {
            get
            {                
                if (isGoodmerge.HasValue)
                    return isGoodmerge.Value;    
            
                isGoodmerge = false;
                if (CurrentProfile == null || !CurrentProfile.EnableGoodmerge)
                    return false;

                string lPath = CurrentDisc.Path;
                if (string.IsNullOrEmpty(lPath))
                    return false;

                int index = lPath.LastIndexOf('.');
                if (index < 0)
                    return false; //file doesn't have an extension

                //get file extension
                string extension = lPath.Substring(index, lPath.Length - index).ToLower();
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

        public string SearchTitle { get; set; }

        internal void SaveInfoCheckedStatus()
        {
            Commit();
        }

        public void SaveGamePlayInfo()
        {
            Commit();
        }

        public void UpdateAndSaveGamePlayInfo()
        {
            playCount++;
            Latestplay = DateTime.Now;
            Commit();
        }

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

        public int CompareTo(Game other)
        {
            if (other == null)
                return 1;
            return this.title.CompareTo(other.title);
        }

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
    }
}
