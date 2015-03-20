using Emulators.MediaPortal2.PathBrowser;
using MediaPortal.Common.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.MediaPortal2.Models
{
    public class ConfigureProfileModel
    {
        #region Members

        protected AbstractProperty _emulatorPathProperty = new WProperty(typeof(string), null);
        protected AbstractProperty _argumentsProperty = new WProperty(typeof(string), null);
        protected AbstractProperty _workingDirectoryProperty = new WProperty(typeof(string), null);
        protected AbstractProperty _launchedFileProperty = new WProperty(typeof(string), null);
        protected AbstractProperty _preCommandProperty = new WProperty(typeof(string), null);
        protected AbstractProperty _preCommandWaitForExitProperty = new WProperty(typeof(bool), true);
        protected AbstractProperty _preCommandShowWindowProperty = new WProperty(typeof(bool), false);
        protected AbstractProperty _postCommandProperty = new WProperty(typeof(string), null);
        protected AbstractProperty _postCommandWaitForExitProperty = new WProperty(typeof(bool), true);
        protected AbstractProperty _postCommandShowWindowProperty = new WProperty(typeof(bool), false);
        protected AbstractProperty _useQuotesProperty = new WProperty(typeof(bool), true);
        protected AbstractProperty _escToExitProperty = new WProperty(typeof(bool), false);
        protected AbstractProperty _warnNoControllersProperty = new WProperty(typeof(bool), false);
        protected AbstractProperty _enableGoodmergeProperty = new WProperty(typeof(bool), false);
        protected AbstractProperty _goodmergeTagsProperty = new WProperty(typeof(string), null);
        
        object syncRoot = new object();
        PathBrowserHandler pathBrowserHandler = new PathBrowserHandler();
        EmulatorProfile currentProfile;

        #endregion

        #region GUI Properties

        public AbstractProperty EmulatorPathProperty { get { return _emulatorPathProperty; } }
        public string EmulatorPath
        {
            get { return (string)_emulatorPathProperty.GetValue(); }
            set { _emulatorPathProperty.SetValue(value); }
        }

        public AbstractProperty ArgumentsProperty { get { return _argumentsProperty; } }
        public string Arguments
        {
            get { return (string)_argumentsProperty.GetValue(); }
            set { _argumentsProperty.SetValue(value); }
        }

        public AbstractProperty WorkingDirectoryProperty { get { return _workingDirectoryProperty; } }
        public string WorkingDirectory
        {
            get { return (string)_workingDirectoryProperty.GetValue(); }
            set { _workingDirectoryProperty.SetValue(value); }
        }

        public AbstractProperty LaunchedFileProperty { get { return _launchedFileProperty; } }
        public string LaunchedFile
        {
            get { return (string)_launchedFileProperty.GetValue(); }
            set { _launchedFileProperty.SetValue(value); }
        }

        public AbstractProperty PreCommandProperty { get { return _preCommandProperty; } }
        public string PreCommand
        {
            get { return (string)_preCommandProperty.GetValue(); }
            set { _preCommandProperty.SetValue(value); }
        }

        public AbstractProperty PreCommandWaitForExitProperty { get { return _preCommandWaitForExitProperty; } }
        public bool PreCommandWaitForExit
        {
            get { return (bool)_preCommandWaitForExitProperty.GetValue(); }
            set { _preCommandWaitForExitProperty.SetValue(value); }
        }

        public AbstractProperty PreCommandShowWindowProperty { get { return _preCommandShowWindowProperty; } }
        public bool PreCommandShowWindow
        {
            get { return (bool)_preCommandShowWindowProperty.GetValue(); }
            set { _preCommandShowWindowProperty.SetValue(value); }
        }

        public AbstractProperty PostCommandProperty { get { return _postCommandProperty; } }
        public string PostCommand
        {
            get { return (string)_postCommandProperty.GetValue(); }
            set { _postCommandProperty.SetValue(value); }
        }

        public AbstractProperty PostCommandWaitForExitProperty { get { return _postCommandWaitForExitProperty; } }
        public bool PostCommandWaitForExit
        {
            get { return (bool)_postCommandWaitForExitProperty.GetValue(); }
            set { _postCommandWaitForExitProperty.SetValue(value); }
        }

        public AbstractProperty PostCommandShowWindowProperty { get { return _postCommandShowWindowProperty; } }
        public bool PostCommandShowWindow
        {
            get { return (bool)_postCommandShowWindowProperty.GetValue(); }
            set { _postCommandShowWindowProperty.SetValue(value); }
        }

        public AbstractProperty UseQuotesProperty { get { return _useQuotesProperty; } }
        public bool UseQuotes
        {
            get { return (bool)_useQuotesProperty.GetValue(); }
            set { _useQuotesProperty.SetValue(value); }
        }

        public AbstractProperty EscToExitProperty { get { return _escToExitProperty; } }
        public bool EscToExit
        {
            get { return (bool)_escToExitProperty.GetValue(); }
            set { _escToExitProperty.SetValue(value); }
        }

        public AbstractProperty WarnNoControllersProperty { get { return _warnNoControllersProperty; } }
        public bool WarnNoControllers
        {
            get { return (bool)_warnNoControllersProperty.GetValue(); }
            set { _warnNoControllersProperty.SetValue(value); }
        }

        public AbstractProperty EnableGoodmergeProperty { get { return _enableGoodmergeProperty; } }
        public bool EnableGoodmerge
        {
            get { return (bool)_enableGoodmergeProperty.GetValue(); }
            set { _enableGoodmergeProperty.SetValue(value); }
        }

        public AbstractProperty GoodmergeTagsProperty { get { return _goodmergeTagsProperty; } }
        public string GoodmergeTags
        {
            get { return (string)_goodmergeTagsProperty.GetValue(); }
            set { _goodmergeTagsProperty.SetValue(value); }
        }

        #endregion

        #region Public Methods

        public void SetProfile(EmulatorProfile profile)
        {
            if (profile == null)
            {
                Reset();
            }
            else if (profile != currentProfile)
            {
                currentProfile = profile;
                EmulatorPath = profile.EmulatorPath;
                Arguments = profile.Arguments;
                WorkingDirectory = profile.WorkingDirectory;
                UseQuotes = profile.UseQuotes;
                EscToExit = profile.EscapeToExit;
                LaunchedFile = currentProfile.LaunchedExe;
                PreCommand = currentProfile.PreCommand;
                PreCommandWaitForExit = currentProfile.PreCommandWaitForExit;
                PreCommandShowWindow = currentProfile.PreCommandShowWindow;
                PostCommand = currentProfile.PostCommand;
                PostCommandWaitForExit = currentProfile.PostCommandWaitForExit;
                PostCommandShowWindow = currentProfile.PostCommandShowWindow;
                WarnNoControllers = currentProfile.CheckController;
                EnableGoodmerge = profile.EnableGoodmerge;
                GoodmergeTags = currentProfile.GoodmergeTags;
            }
        }

        public void SaveProfile()
        {
            if (currentProfile != null)
            {
                currentProfile.EmulatorPath = EmulatorPath;
                currentProfile.Arguments = Arguments;
                currentProfile.WorkingDirectory = WorkingDirectory;
                currentProfile.UseQuotes = UseQuotes;
                currentProfile.EscapeToExit = EscToExit;
                currentProfile.LaunchedExe = LaunchedFile;
                currentProfile.PreCommand = PreCommand;
                currentProfile.PreCommandWaitForExit = PreCommandWaitForExit;
                currentProfile.PreCommandShowWindow = PreCommandShowWindow;
                currentProfile.PostCommand = PostCommand;
                currentProfile.PostCommandWaitForExit = PostCommandWaitForExit;
                currentProfile.PostCommandShowWindow = PostCommandShowWindow;
                currentProfile.CheckController = WarnNoControllers;
                currentProfile.EnableGoodmerge = EnableGoodmerge;
                currentProfile.GoodmergeTags = GoodmergeTags;
                currentProfile.Commit();
            }
        }

        public void Reset()
        {
            currentProfile = null;
            EmulatorPath = null;
            Arguments = null;
            WorkingDirectory = null;
            UseQuotes = true;
            EscToExit = false;
            LaunchedFile = null;
            PreCommand = null;
            PreCommandWaitForExit = true;
            PreCommandShowWindow = false;
            PostCommand = null;
            PostCommandWaitForExit = true;
            PostCommandShowWindow = false;
            WarnNoControllers = false;
            EnableGoodmerge = false;
            GoodmergeTags = null;
        }

        public void ChooseEmulatorPath()
        {
            pathBrowserHandler.ShowPathSelect("[Emulators.EmulatorPath]", EmulatorPath, true, p => p.IsExecutable(), p => EmulatorPath = p);
        }

        public void ChooseWorkingDirectory()
        {
            pathBrowserHandler.ShowPathSelect("[Emulators.WorkingDirectory]", WorkingDirectory, false, p => !string.IsNullOrEmpty(p), p => WorkingDirectory = p);
        }

        #endregion
    }
}
