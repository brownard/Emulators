using Emulators.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Emulators
{
    [DBTable("GameDiscs")]
    public class GameDisc : DBItem, IComparable<GameDisc>
    {
        public GameDisc() { }
        public GameDisc(string path, string launchFile = null)
        {
            Path = path;
            this.launchFile = launchFile;
        }

        [DBField]
        public int Number
        {
            get { return number; }
            set
            {
                number = value;
                CommitNeeded = true;
            }
        }
        int number = 1;

        [DBField]
        public string Path
        {
            get { return path; }
            set 
            { 
                path = value;
                if (!string.IsNullOrEmpty(value))
                    filename = System.IO.Path.GetFileNameWithoutExtension(value);
                CommitNeeded = true;            
            }
        }
        string path = null;

        [DBField]
        public string LaunchFile
        {
            get { return launchFile; }
            set
            {
                launchFile = value;
                CommitNeeded = true;
            }
        }
        string launchFile = null;

        public List<string> GoodmergeFiles
        {
            get;
            set;
        }

        public string Filename
        {
            get { return filename; }
        }
        string filename = "";

        public string Name
        {
            get { return string.Format("Disc {0}", number); }
        }

        #region IComparable<GameDisc> Members

        public int CompareTo(GameDisc other)
        {
            if (other == null)
                return 1;
            return this.number.CompareTo(other.number);
        }

        #endregion

        public bool Selected { get; set; }
    }
}
