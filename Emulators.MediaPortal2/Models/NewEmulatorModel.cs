using Emulators.AutoConfig;
using MediaPortal.Common;
using MediaPortal.Common.Commands;
using MediaPortal.Common.General;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Utilities;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Emulators.MediaPortal2
{
    public class NewEmulatorModel : IWorkflowModel
    {
        #region Protected Members

        protected AbstractProperty _emulatorPathProperty = new WProperty(typeof(string), null);
        protected AbstractProperty _nameProperty = new WProperty(typeof(string), null);
        protected AbstractProperty _platformProperty = new WProperty(typeof(string), null);
        protected AbstractProperty _romDirectoryProperty = new WProperty(typeof(string), null);
        protected AbstractProperty _filtersProperty = new WProperty(typeof(string), null);
        protected AbstractProperty _isEmulatorValidProperty = new WProperty(typeof(bool), false);

        #endregion

        EmulatorConfig currentConfig;
        PathBrowserCloseWatcher _pathBrowserCloseWatcher = null;

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

        public AbstractProperty IsEmulatorValidProperty { get { return _isEmulatorValidProperty; } }
        public bool IsEmulatorValid
        {
            get { return (bool)_isEmulatorValidProperty.GetValue(); }
            set { _isEmulatorValidProperty.SetValue(value); }
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

        public void ChoosePlatform()
        {
            ItemsList items = new ItemsList();
            var platforms = Dropdowns.GetPlatformList();
            for (int i = -1; i < platforms.Count; i++)
            {
                string platformName = i < 0 ? "" : platforms[i].Name;
                items.Add(new ListItem(Consts.KEY_NAME, platformName)
                    {
                        Selected = Platform == platformName,
                        Command = new MethodDelegateCommand(() => { Platform = platformName; })
                    });
            }

            var dialog = (ListDialogModel)ServiceRegistration.Get<IWorkflowManager>().GetModel(Guids.ListDialogModel);
            dialog.ShowDialog("[Emulators.SelectPlatform]", items);
        }

        public void FinishEmulatorConfig()
        {
            Emulator newEmulator = Emulator.CreateNewEmulator();
            newEmulator.Title = Name;
            newEmulator.Platform = Platform;
            newEmulator.Filter = Filters;
            newEmulator.PathToRoms = RomDirectory;
            newEmulator.DefaultProfile.EmulatorPath = EmulatorPath;
            if (currentConfig != null)
            {
                newEmulator.CaseAspect = currentConfig.CaseAspect;
                if (currentConfig.ProfileConfig != null)
                {
                    newEmulator.DefaultProfile.Arguments = currentConfig.ProfileConfig.Arguments;
                    newEmulator.DefaultProfile.WorkingDirectory = currentConfig.ProfileConfig.WorkingDirectory;
                    if (currentConfig.ProfileConfig.UseQuotes.HasValue)
                        newEmulator.DefaultProfile.UseQuotes = currentConfig.ProfileConfig.UseQuotes.Value;
                    if (currentConfig.ProfileConfig.EscapeToExit.HasValue)
                        newEmulator.DefaultProfile.EscapeToExit = currentConfig.ProfileConfig.EscapeToExit.Value;
                }
            }
            newEmulator.Commit();
            IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
            workflowManager.NavigatePopToState(Guids.NewEmulatorState, true);
            var platformLookup = new Models.PlatformDetailsDialog(newEmulator.Platform, newEmulator);
            platformLookup.GetPlatformInfo();
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

        void reset()
        {
            currentConfig = null;
            EmulatorPath = null;
            Name = null;
            Platform = null;
            Filters = null;
            RomDirectory = null;
        }

        void updateValidStatus()
        {
            IsEmulatorValid = !string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(EmulatorPath);
        }

        void updateConfig(string emulatorPath)
        {
            currentConfig = EmuAutoConfig.Instance.CheckForSettings(emulatorPath);
            if (currentConfig != null)
            {
                if (string.IsNullOrEmpty(Name))
                    Name = currentConfig.Platform;
                if (string.IsNullOrEmpty(Platform))
                    Platform = currentConfig.Platform;
                if (string.IsNullOrEmpty(Filters))
                    Filters = currentConfig.Filters;
            }
        }

        #region IWorkflow

        public bool CanEnterState(MediaPortal.UI.Presentation.Workflow.NavigationContext oldContext, MediaPortal.UI.Presentation.Workflow.NavigationContext newContext)
        {
            return true;
        }

        public void ChangeModelContext(MediaPortal.UI.Presentation.Workflow.NavigationContext oldContext, MediaPortal.UI.Presentation.Workflow.NavigationContext newContext, bool push)
        {
            
        }

        public void Deactivate(MediaPortal.UI.Presentation.Workflow.NavigationContext oldContext, MediaPortal.UI.Presentation.Workflow.NavigationContext newContext)
        {
            
        }

        public void EnterModelContext(MediaPortal.UI.Presentation.Workflow.NavigationContext oldContext, MediaPortal.UI.Presentation.Workflow.NavigationContext newContext)
        {
            reset();
        }

        public void ExitModelContext(MediaPortal.UI.Presentation.Workflow.NavigationContext oldContext, MediaPortal.UI.Presentation.Workflow.NavigationContext newContext)
        {
            
        }

        public Guid ModelId
        {
            get { return Guids.NewEmulatorWorkflow; }
        }

        public void Reactivate(MediaPortal.UI.Presentation.Workflow.NavigationContext oldContext, MediaPortal.UI.Presentation.Workflow.NavigationContext newContext)
        {
            
        }

        public void UpdateMenuActions(MediaPortal.UI.Presentation.Workflow.NavigationContext context, IDictionary<Guid, MediaPortal.UI.Presentation.Workflow.WorkflowAction> actions)
        {
            
        }

        public ScreenUpdateMode UpdateScreen(MediaPortal.UI.Presentation.Workflow.NavigationContext context, ref string screen)
        {
            return ScreenUpdateMode.AutoWorkflowManager;
        }

        #endregion
    }
}
