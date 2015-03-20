using Emulators.AutoConfig;
using Emulators.MediaPortal2.ListItems;
using Emulators.MediaPortal2.Models.Dialogs;
using Emulators.MediaPortal2.PathBrowser;
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

        protected AbstractProperty _nameProperty = new WProperty(typeof(string), null);
        protected AbstractProperty _platformProperty = new WProperty(typeof(string), null);
        protected AbstractProperty _romDirectoryProperty = new WProperty(typeof(string), null);
        protected AbstractProperty _filtersProperty = new WProperty(typeof(string), null);
        protected AbstractProperty _caseAspectProperty = new WProperty(typeof(double), 0.0);
        protected AbstractProperty _isEmulatorValidProperty = new WProperty(typeof(bool), false);

        protected AbstractProperty _profileModelProperty = new WProperty(typeof(ConfigureProfileModel), new ConfigureProfileModel());

        PathBrowserHandler pathBrowserHandler = new PathBrowserHandler();
        DialogCloseWatcher confirmDeleteCloseWatcher;
        object syncRoot = new object();
        Emulator currentEmulator;

        #endregion

        public ConfigureEmulatorModel()
        {
            ProfileModel.EmulatorPathProperty.Attach(emulatorPathChangedHandler);
        }

        void emulatorPathChangedHandler(AbstractProperty property, object oldValue)
        {
            updateConfig(property.GetValue() as string);
            updateValidStatus();
        }

        #region GUI Properties

        public ItemsList Emulators { get; set; }
        public ItemsList CurrentProfiles { get; set; }

        public AbstractProperty ProfileModelProperty { get { return _profileModelProperty; } }
        public ConfigureProfileModel ProfileModel
        {
            get { return (ConfigureProfileModel)_profileModelProperty.GetValue(); }
            set { _profileModelProperty.SetValue(value); }
        }

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
            Name = null;
            Platform = null;
            Filters = null;
            RomDirectory = null;
            CaseAspect = 0;
            ProfileModel.Reset();
        }

        public void ChooseRomDirectory()
        {
            pathBrowserHandler.ShowPathSelect("[Emulators.RomDirectory]", RomDirectory, false, p => !string.IsNullOrEmpty(p), p => RomDirectory = p);
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
            ProfileModel.SaveProfile();
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
                setupProfileList();
                ProfileModel.SetProfile(currentEmulator.DefaultProfile);
            }
            else
            {
                object emuObject;
                if (newContext.ContextVariables.TryGetValue(KEY_EMULATOR_EDIT, out emuObject))
                    setupEmulator(emuObject as Emulator);
                if (newContext.ContextVariables.TryGetValue(KEY_PROFILE_EDIT, out emuObject))
                    ProfileModel.SetProfile(emuObject as EmulatorProfile);
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
            ItemsList items = Emulators;
            bool fireChange = createOrResetList(ref items);

            var emulators = Emulator.GetAll();
            for (int i = 0; i < emulators.Count; i++)
            {
                Emulator emulator = emulators[i];
                items.Add(new ContextListItem(Consts.KEY_NAME, emulator.Title)
                {
                    Command = new MethodDelegateCommand(() => EditEmulator(emulator)),
                    ContextCommand = new MethodDelegateCommand(() => showEmulatorContext(emulator))
                });
            }

            if (fireChange)
                items.FireChange();
            else
                Emulators = items;
        }

        void showEmulatorContext(Emulator emulator)
        {
            ItemsList items = new ItemsList();
            items.Add(new ListItem(Consts.KEY_NAME, "[Emulators.Profiles]")
            {
                Command = new MethodDelegateCommand(() => ShowProfileSelectDialog(emulator))
            });
            items.Add(new ListItem(Consts.KEY_NAME, "[Emulators.Dialogs.Delete]")
            {
                Command = new MethodDelegateCommand(() => confirmEmulatorDelete(emulator))
            });
            ListDialogModel.Instance().ShowDialog(emulator.Title, items);
        }

        void confirmEmulatorDelete(Emulator emulator)
        {
            IDialogManager dialogManager = ServiceRegistration.Get<IDialogManager>();
            Guid dialogHandle = dialogManager.ShowDialog(emulator.Title, "[Emulators.Dialogs.ConfirmDelete]", DialogType.YesNoDialog, false, DialogButtonType.No);
            confirmDeleteCloseWatcher = new DialogCloseWatcher(this, dialogHandle, dialogResult => 
            {
                if (dialogResult == DialogResult.Yes)
                    emulator.Delete();
            });
        }

        static bool createOrResetList(ref ItemsList items)
        {
            if (items == null)
            {
                items = new ItemsList();
                return false;
            }
            else
            {
                items.Clear();
                return true;
            }
        }

        void setupEmulator(Emulator emulator)
        {
            if (emulator != null && emulator != currentEmulator)
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

        void updateValidStatus()
        {
            IsEmulatorValid = !string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(ProfileModel.EmulatorPath);
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
                    ConfigureProfileModel profileModel = ProfileModel;

                    if (string.IsNullOrEmpty(profileModel.Arguments))
                        profileModel.Arguments = profileConfig.Arguments;
                    if (string.IsNullOrEmpty(profileModel.WorkingDirectory))
                        profileModel.WorkingDirectory = profileConfig.WorkingDirectory;
                    if (profileConfig.UseQuotes.HasValue)
                        profileModel.UseQuotes = profileConfig.UseQuotes.Value;
                    if (profileConfig.EscapeToExit.HasValue)
                        profileModel.EscToExit = emulatorConfig.ProfileConfig.EscapeToExit.Value;
                }
            }
        }

        void restartImporter()
        {
            IEmulatorsService emulatorsService = ServiceRegistration.Get<IEmulatorsService>();
            emulatorsService.Importer.Restart();
        }

        protected void ReleaseModelData()
        {
            Emulators = null;
            CurrentProfiles = null;
            Reset();
            pathBrowserHandler.Dispose();
            if (confirmDeleteCloseWatcher != null)
            {
                confirmDeleteCloseWatcher.Dispose();
                confirmDeleteCloseWatcher = null;
            }
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
            ReleaseModelData();
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