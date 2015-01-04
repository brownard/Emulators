using Emulators.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Emulators
{
    [DBTable("EmulatorProfiles")]
    public class EmulatorProfile : DBItem
    {
        public EmulatorProfile() { }

        public EmulatorProfile(bool isDefault)
        {
            if (isDefault)
            {
                this.IsDefault = true;
                this.Title = "Default";
            }
            else
                this.Title = "New Profile";
        }

        #region Properties

        [DBField]
        public bool IsDefault
        {
            get { return isDefault; }
            set
            {
                isDefault = value;
                CommitNeeded = true;
            }
        }
        bool isDefault = false;

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

        [DBField]
        public string EmulatorPath
        {
            get { return emulatorPath; }
            set
            {
                emulatorPath = value;
                CommitNeeded = true;
            }
        }
        string emulatorPath = null;

        [DBField]
        public string WorkingDirectory
        {
            get { return workingDirectory; }
            set
            {
                workingDirectory = value;
                CommitNeeded = true;
            }
        }
        string workingDirectory = null;

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
        string arguments = null;

        [DBField]
        public bool UseQuotes
        {
            get { return useQuotes; }
            set
            {
                useQuotes = value;
                CommitNeeded = true;
            }
        }
        bool useQuotes = true;

        [DBField]
        public bool SuspendMP
        {
            get { return suspendMP; }
            set
            {
                suspendMP = value;
                CommitNeeded = true;
            }
        }
        bool suspendMP = false;

        [DBField]
        public bool EnableGoodmerge
        {
            get { return enableGoodmerge; }
            set
            {
                enableGoodmerge = value;
                CommitNeeded = true;
            }
        }
        bool enableGoodmerge = false;

        [DBField]
        public string GoodmergeTags
        {
            get { return goodmergeTags; }
            set
            {
                goodmergeTags = value;
                CommitNeeded = true;
            }
        }
        string goodmergeTags = null;

        List<string> tags = null;
        public List<string> GetGoodmergeTags()
        {
            if (tags == null)
            {
                tags = new List<string>();
                if (!string.IsNullOrEmpty(goodmergeTags))
                {
                    string[] sTags = goodmergeTags.Split(';');
                    for (int x = 0; x < sTags.Length; x++)
                    {
                        string tag = sTags[x].Trim();
                        if (tag.Length > 0)
                            tags.Add(tag);
                    }
                }
            }
            return tags;
        }

        [DBField]
        public bool MountImages
        {
            get { return mountImages; }
            set
            {
                mountImages = value;
                CommitNeeded = true;
            }
        }
        bool mountImages = false;

        [DBField]
        public bool EscapeToExit
        {
            get { return escapeToExit; }
            set
            {
                escapeToExit = value;
                CommitNeeded = true;
            }
        }
        bool escapeToExit = false;

        [DBField]
        public bool CheckController
        {
            get { return checkController; }
            set
            {
                checkController = value;
                CommitNeeded = true;
            }
        }
        bool checkController = false;

        [DBField]
        public string PreCommand
        {
            get { return preCommand; }
            set
            {
                preCommand = value;
                CommitNeeded = true;
            }
        }
        string preCommand = null;

        [DBField]
        public bool PreCommandWaitForExit
        {
            get { return preCommandWaitForExit; }
            set
            {
                preCommandWaitForExit = value;
                CommitNeeded = true;
            }
        }
        bool preCommandWaitForExit = false;

        [DBField]
        public bool PreCommandShowWindow
        {
            get { return preCommandShowWindow; }
            set
            {
                preCommandShowWindow = value;
                CommitNeeded = true;
            }
        }
        bool preCommandShowWindow = false;

        [DBField]
        public string PostCommand
        {
            get { return postCommand; }
            set
            {
                postCommand = value;
                CommitNeeded = true;
            }
        }
        string postCommand = null;

        [DBField]
        public bool PostCommandWaitForExit
        {
            get { return postCommandWaitForExit; }
            set
            {
                postCommandWaitForExit = value;
                CommitNeeded = true;
            }
        }
        bool postCommandWaitForExit = false;

        [DBField]
        public bool PostCommandShowWindow
        {
            get { return postCommandShowWindow; }
            set
            {
                postCommandShowWindow = value;
                CommitNeeded = true;
            }
        }
        bool postCommandShowWindow = false;

        [DBField]
        public string LaunchedExe
        {
            get { return launchedExe; }
            set
            {
                launchedExe = value;
                CommitNeeded = true;
            }
        }
        string launchedExe = null;

        [DBField]
        public bool? StopEmulationOnKey
        {
            get { return stopEmulationOnKey; }
            set
            {
                stopEmulationOnKey = value;
                CommitNeeded = true;
            }
        }
        bool? stopEmulationOnKey = null;

        [DBField]
        public bool DelayResume
        {
            get { return delayResume; }
            set
            {
                delayResume = value;
                CommitNeeded = true;
            }
        }
        bool delayResume = false;

        [DBField]
        public int ResumeDelay
        {
            get { return resumeDelay; }
            set
            {
                resumeDelay = value;
                CommitNeeded = true;
            }
        }
        int resumeDelay = 0;

        #endregion
    }
}