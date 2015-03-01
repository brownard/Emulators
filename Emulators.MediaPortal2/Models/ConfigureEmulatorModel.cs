using Emulators.AutoConfig;
using Emulators.MediaPortal2.Models.Dialogs;
using MediaPortal.Common;
using MediaPortal.Common.Commands;
using MediaPortal.Common.General;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Utilities;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Emulators.MediaPortal2.Models
{
    public class ConfigureEmulatorModel : IWorkflowModel
    {
        #region Consts

        public const string KEY_EMULATOR_EDIT = "EmulatorsEditEmulator";
        public const string KEY_PROFILE_EDIT = "EmulatorsEditProfile";

        #endregion

        #region Static Methods

        public static ConfigureEmulatorModel Instance()
        {
            IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
            return (ConfigureEmulatorModel)workflowManager.GetModel(Guids.ConfigureEmulatorModel);
        }

        public static void ShowProfileSelectDialog(Emulator emulator)
        {
            var workflow = ServiceRegistration.Get<IWorkflowManager>();
            workflow.NavigatePush(Guids.ProfileSelectState, createNavigationConfig(KEY_EMULATOR_EDIT, emulator, null));
        }

        public static void EditEmulator(Emulator emulator)
        {
            var workflow = ServiceRegistration.Get<IWorkflowManager>();
            workflow.NavigatePush(Guids.EditEmulatorState, createNavigationConfig(KEY_EMULATOR_EDIT, emulator, emulator.Title));
        }

        public static void EditProfile(EmulatorProfile profile)
        {
            var workflow = ServiceRegistration.Get<IWorkflowManager>();
            workflow.NavigatePush(Guids.EditProfileState, createNavigationConfig(KEY_PROFILE_EDIT, profile, profile.Title));
        }

        static NavigationContextConfig createNavigationConfig(string key, object value, string displayLabel)
        {
            Dictionary<string, object> contextData = new Dictionary<string, object>();
            contextData[key] = value;
            return new NavigationContextConfig() { AdditionalContextVariables = contextData, NavigationContextDisplayLabel = displayLabel };
        }

        #endregion

        #region Members

        protected AbstractProperty _emulatorPathProperty = new WProperty(typeof(string), null);
        protected AbstractProperty _nameProperty = new WProperty(typeof(string), null);
        protected AbstractProperty _platformProperty = new WProperty(typeof(string), null);
        protected AbstractProperty _romDirectoryProperty = new WProperty(typeof(string), null);
        protected AbstractProperty _filtersProperty = new WProperty(typeof(string), null);
        protected AbstractProperty _enableGoodmergeProperty = new WProperty(typeof(bool), false);
        protected AbstractProperty _isEmulatorValidProperty = new WProperty(typeof(bool), false);

        protected AbstractProperty _caseAspectProperty = new WProperty(typeof(double), 0.0);
        protected AbstractProperty _argumentsProperty = new WProperty(typeof(string), null);
        protected AbstractProperty _workingDirectoryProperty = new WProperty(typeof(string), null);
        protected AbstractProperty _launchedFileProperty = new WProperty(typeof(string), null);
        protected AbstractProperty _preCommandProperty = new WProperty(typeof(string), null);
        protected AbstractProperty _preCommandWaitForExitProperty = new WProperty(typeof(bool), true);
        protected AbstractProperty _preCommandShowWindowProperty = new WProperty(typeof(bool), false);
        protected AbstractProperty _postCommandProperty = new WProperty(typeof(string), null);
        protected AbstractProperty _postCommandWaitForExitProperty = new WProperty(typeof(bool), true);
        protected AbstractProperty _postCommandShowWindowProperty = new WProperty(typeof(bool), false);
        protected AbstractProperty _useQuotesProperty = new WProperty(typeof(bool), false);
        protected AbstractProperty _escToExitProperty = new WProperty(typeof(bool), false);
        protected AbstractProperty _warnNoControllersProperty = new WProperty(typeof(bool), false);
        protected AbstractProperty _goodmergeTagsProperty = new WProperty(typeof(string), null);

        PathBrowserCloseWatcher _pathBrowserCloseWatcher;
        object syncRoot = new object();
        Emulator currentEmulator;
        EmulatorProfile currentProfile;

        #endregion

        #region GUI Properties

        public ItemsList Emulators { get; set; }
        public ItemsList CurrentProfiles { get; set; }

        public AbstractProperty NameProperty { get { return _nameProperty; } }
        public string Name
        {
            get { return (string)_nameProperty.GetValue(); }
            set
            {
                _nameProperty.SetValue(value);
                updateValidStatus();
            }
        }

        public AbstractProperty PlatformProperty { get { return _platformProperty; } }
        public string Platform
        {
            get { return (string)_platformProperty.GetValue(); }
            set { _platformProperty.SetValue(value); }
        }

        public AbstractProperty EmulatorPathProperty { get { return _emulatorPathProperty; } }
        public string EmulatorPath
        {
            get { return (string)_emulatorPathProperty.GetValue(); }
            set
            {
                _emulatorPathProperty.SetValue(value);
                updateValidStatus();
            }
        }

        public AbstractProperty RomDirectoryProperty { get { return _romDirectoryProperty; } }
        public string RomDirectory
        {
            get { return (string)_romDirectoryProperty.GetValue(); }
            set { _romDirectoryProperty.SetValue(value); }
        }

        public AbstractProperty FiltersProperty { get { return _filtersProperty; } }
        public string Filters
        {
            get { return (string)_filtersProperty.GetValue(); }
            set { _filtersProperty.SetValue(value); }
        }

        public AbstractProperty CaseAspectProperty { get { return _caseAspectProperty; } }
        public double CaseAspect
        {
            get { return (double)_caseAspectProperty.GetValue(); }
            set { _caseAspectProperty.SetValue(value); }
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

        public AbstractProperty IsEmulatorValidProperty { get { return _isEmulatorValidProperty; } }
        public bool IsEmulatorValid
        {
            get { return (bool)_isEmulatorValidProperty.GetValue(); }
            set { _isEmulatorValidProperty.SetValue(value); }
        }

        #endregion

        #region Public Methods

        protected void Reset()
        {
            currentEmulator = null;
            currentProfile = null;
            EmulatorPath = null;
            Name = null;
            Platform = null;
            Filters = null;
            RomDirectory = null;
            CaseAspect = 0;
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
            doPathSelect("[Emulators.EmulatorPath]", EmulatorPath, true, p => p.IsExecutable(), p =>
                {
                    EmulatorPath = p;
                    updateConfig(p);
                });
        }

        public void ChooseRomDirectory()
        {
            doPathSelect("[Emulators.RomDirectory]", RomDirectory, false, p => !string.IsNullOrEmpty(p), p => RomDirectory = p);
        }

        public void ChooseWorkingDirectory()
        {
            doPathSelect("[Emulators.WorkingDirectory]", WorkingDirectory, false, p => !string.IsNullOrEmpty(p), p => WorkingDirectory = p);
        }

        public void ChoosePlatform()
        {
            IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
            screenManager.ShowDialog(Consts.DIALOG_PLATFORM_SELECT);
        }

        public void SaveNewEmulator()
        {
            if (currentEmulator == null)
                return;

            SaveProfile();
            SaveEmulator();

            IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
            workflowManager.NavigatePopToState(Guids.NewEmulatorState, true);
            PlatformDetailsModel.GetPlatformInfo(currentEmulator.Platform, currentEmulator);
        }

        public void SaveEmulator()
        {
            if (currentEmulator != null)
            {
                bool reImport = currentEmulator.PathToRoms != RomDirectory;
                if (reImport)
                    currentEmulator.PathToRoms = RomDirectory;
                currentEmulator.Title = Name;
                currentEmulator.Platform = Platform;
                currentEmulator.Filter = Filters;
                currentEmulator.CaseAspect = CaseAspect;
                currentEmulator.Commit();
                getEmulators();
                if (reImport)
                    restartImporter();
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

        #endregion

        #region Private Methods

        void updateState(NavigationContext newContext)
        {
            if (Emulators == null)
                getEmulators();

            Guid stateId = newContext.WorkflowState.StateId;
            if (stateId == Guids.NewEmulatorState)
            {
                Reset();
                currentEmulator = Emulator.CreateNewEmulator();
                currentProfile = currentEmulator.DefaultProfile;
            }
            else if (stateId == Guids.EditEmulatorState || stateId == Guids.EditProfileState)
            {
                Reset();
                object emuObject;
                if (newContext.ContextVariables.TryGetValue(KEY_EMULATOR_EDIT, out emuObject))
                    setupEmulator(emuObject as Emulator);
                if (newContext.ContextVariables.TryGetValue(KEY_PROFILE_EDIT, out emuObject))
                    setupProfile(emuObject as EmulatorProfile);
            }
        }

        void saveOldState(NavigationContext oldContext, bool push)
        {
            if (!push)
            {
                Guid stateId = oldContext.WorkflowState.StateId;
                if (stateId == Guids.EditEmulatorState)
                    SaveEmulator();
                else if (stateId == Guids.EditProfileState)
                    SaveProfile();
            }
        }

        void getEmulators()
        {
            bool fireChange;
            ItemsList items = Emulators;
            if (items == null)
            {
                items = new ItemsList();
                fireChange = false;
            }
            else
            {
                items.Clear();
                fireChange = true;
            }

            var emulators = Emulator.GetAll();
            for (int i = 0; i < emulators.Count; i++)
            {
                Emulator emulator = emulators[i];
                items.Add(new ListItem(Consts.KEY_NAME, emulator.Title)
                {
                    Command = new MethodDelegateCommand(() => EditEmulator(emulator))
                });
            }

            if (fireChange)
                items.FireChange();
            else
                Emulators = items;
        }

        void setupEmulator(Emulator emulator)
        {
            if (emulator != null)
            {
                currentEmulator = emulator;
                Name = emulator.Title;
                Platform = emulator.Platform;
                Filters = emulator.Filter;
                RomDirectory = emulator.PathToRoms;
                CaseAspect = emulator.CaseAspect;

                setupProfileList();
            }
        }

        void setupProfile(EmulatorProfile profile)
        {
            if (profile != null)
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

        void setupProfileList()
        {
            if (currentEmulator != null)
            {
                var profiles = currentEmulator.EmulatorProfiles;
                ItemsList items = new ItemsList();
                for (int i = 0; i < profiles.Count; i++)
                {
                    EmulatorProfile profile = profiles[i];
                    items.Add(new ListItem(Consts.KEY_NAME, profile.Title)
                    {
                        Command = new MethodDelegateCommand(() => EditProfile(profile))
                    });
                }
                CurrentProfiles = items;
            }
        }

        void doPathSelect(string displayLabel, string initialPath, bool fileSelect, Func<string, bool> validate, Action<string> completed)
        {
            initialPath = string.IsNullOrEmpty(initialPath) ? null : DosPathHelper.GetDirectory(initialPath);
            Guid dialogHandle = ServiceRegistration.Get<IPathBrowser>().ShowPathBrowser(displayLabel, fileSelect, false,
                string.IsNullOrEmpty(initialPath) ? null : LocalFsResourceProviderBase.ToResourcePath(initialPath),
                path =>
                {
                    string choosenPath = LocalFsResourceProviderBase.ToDosPath(path.LastPathSegment.Path);
                    return validate(choosenPath);
                });
            if (_pathBrowserCloseWatcher != null)
                _pathBrowserCloseWatcher.Dispose();
            _pathBrowserCloseWatcher = new PathBrowserCloseWatcher(this, dialogHandle, chosenPath =>
            {
                completed(LocalFsResourceProviderBase.ToDosPath(chosenPath));
            },
                null);
        }

        void updateValidStatus()
        {
            IsEmulatorValid = !string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(EmulatorPath);
        }

        void updateConfig(string emulatorPath)
        {
            EmulatorConfig emulatorConfig = EmuAutoConfig.Instance.CheckForSettings(emulatorPath);
            if (emulatorConfig != null)
            {
                if (string.IsNullOrEmpty(Name))
                    Name = emulatorConfig.Platform;
                if (string.IsNullOrEmpty(Platform))
                    Platform = emulatorConfig.Platform;
                if (string.IsNullOrEmpty(Filters))
                    Filters = emulatorConfig.Filters;
                if (CaseAspect == 0)
                    CaseAspect = emulatorConfig.CaseAspect;

                if (emulatorConfig.ProfileConfig != null)
                {
                    ProfileConfig profileConfig = emulatorConfig.ProfileConfig;
                    if (string.IsNullOrEmpty(Arguments))
                        Arguments = profileConfig.Arguments;
                    if (string.IsNullOrEmpty(WorkingDirectory))
                        WorkingDirectory = profileConfig.WorkingDirectory;
                    if (profileConfig.UseQuotes.HasValue)
                        UseQuotes = profileConfig.UseQuotes.Value;
                    if (profileConfig.EscapeToExit.HasValue)
                        EscToExit = emulatorConfig.ProfileConfig.EscapeToExit.Value;
                }
            }
        }

        void restartImporter()
        {
            IEmulatorsService emulatorsService = ServiceRegistration.Get<IEmulatorsService>();
            emulatorsService.Importer.Restart();
        }

        #endregion

        #region IWorkflow

        public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
        {
            return true;
        }

        public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
        {
            saveOldState(oldContext, push);
            updateState(newContext);
        }

        public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
        {

        }

        public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
        {
            updateState(newContext);
        }

        public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
        {
            saveOldState(oldContext, false);
        }

        public Guid ModelId
        {
            get { return Guids.ConfigureEmulatorModel; }
        }

        public void Reactivate(NavigationContext oldContext, NavigationContext newContext)
        {

        }

        public void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
        {

        }

        public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
        {
            return ScreenUpdateMode.AutoWorkflowManager;
        }

        #endregion
    }
}