using Emulators.Database;
using Emulators.MediaPortal2.Navigation;
using MediaPortal.Common;
using MediaPortal.Common.Commands;
using MediaPortal.Common.General;
using MediaPortal.Common.PathManager;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Workflow;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.MediaPortal2
{
    public class EmulatorsWorkflowModel : IWorkflowModel
    {
        #region Static Methods
        
        static NavigationData GetNavigationData(NavigationContext currentContext)
        {
            NavigationContext context = currentContext;
            return context == null ? null : context.GetContextVariable(NavigationData.NAVIGATION_DATA, false) as NavigationData;
        }

        static NavigationContextConfig GetContextConfig(NavigationData navigationData)
        {
            if (navigationData == null)
                return null;

            Dictionary<string, object> contextData = new Dictionary<string, object>();
            contextData[NavigationData.NAVIGATION_DATA] = navigationData;
            return new NavigationContextConfig()
            {
                NavigationContextDisplayLabel = navigationData.DisplayName,
                AdditionalContextVariables = contextData
            };
        }

        static void PushState(Guid stateId, NavigationData navigationData)
        {
            IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
            workflowManager.NavigatePush(stateId, GetContextConfig(navigationData));
        }

        static void PushTransientState(string name, string displayLabel, NavigationData navigationData)
        {
            WorkflowState newState = WorkflowState.CreateTransientState(name, displayLabel, false, "emulators", true, WorkflowType.Workflow);
            IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
            workflowManager.NavigatePushTransient(newState, GetContextConfig(navigationData));
        }

        #endregion

        #region Members

        protected AbstractProperty _focusedItemProperty;
        protected AbstractProperty _layoutTypeProperty;

        NavigationContext currentContext;

        #endregion

        #region Ctor/Dtor

        public EmulatorsWorkflowModel()
        {
            EmulatorsCore.Init(new EmulatorsSettings());
            _layoutTypeProperty = new WProperty(typeof(LayoutType), LayoutType.List);
            _focusedItemProperty = new WProperty(typeof(ItemViewModel), null);
        }

        ~EmulatorsWorkflowModel()
        {
            EmulatorsCore.Options.Save();
        }

        #endregion

        #region Public Properties

        public NavigationData NavigationData
        {
            get { return GetNavigationData(currentContext); }
        }

        public ItemsList Items
        {
            get
            {
                NavigationData navigationData = NavigationData;
                return navigationData != null ? navigationData.ItemsList : null;
            }
        }

        public string Header
        {
            get
            {
                NavigationData navigationData = NavigationData;
                return navigationData != null ? navigationData.DisplayName : null;
            }
        }

        public AbstractProperty FocusedItemProperty { get { return _focusedItemProperty; } }
        public ItemViewModel FocusedItem
        {
            get { return (ItemViewModel)_focusedItemProperty.GetValue(); }
            set
            {
                if (value != null)
                {
                    _focusedItemProperty.SetValue(value);
                }
            }
        }

        public AbstractProperty LayoutTypeProperty { get { return _layoutTypeProperty; } }
        public LayoutType LayoutType
        {
            get { return (LayoutType)_layoutTypeProperty.GetValue(); }
            set { _layoutTypeProperty.SetValue(value); }
        }

        #endregion

        #region Public Methods

        public void SelectItem(ItemViewModel item)
        {
            ICommand command = item.Command;
            if (command != null)
                command.Execute();
        }

        public void ShowContext(ItemViewModel item)
        {
            ICommand command = item.ContextCommand;
            if (command != null)
                command.Execute();
        }

        public void EmulatorSelected(Emulator emulator)
        {
            ItemsList items = new ItemsList();
            foreach (Game game in emulator.Games)
                items.Add(new GameViewModel(game, this));

            NavigationData navigationData = new NavigationData() { DisplayName = "[Emulators.Games]", ItemsList = items };
            PushTransientState("emuGames", navigationData.DisplayName, navigationData);
        }

        public void GroupSelected(RomGroup group)
        {
            ItemsList items = new ItemsList();
            foreach (DBItem item in group.GroupItems)
            {
                Game game = item as Game;
                if (game != null)
                {
                    items.Add(new GameViewModel(game, this));
                    continue;
                }

                RomGroup subGroup = item as RomGroup;
                if (subGroup != null)
                {
                    items.Add(new GroupViewModel(subGroup, this));
                    continue;
                }

                Emulator emulator = item as Emulator;
                if (emulator != null)
                    items.Add(new EmulatorViewModel(emulator, this));
            }

            NavigationData navigationData = new NavigationData() { DisplayName = group.Title, ItemsList = items };
            PushTransientState("emuSubGroup", navigationData.DisplayName, navigationData);
        }

        public void GameSelected(Game game)
        {
            IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
            GameLaunchWorkflowModel launchDlg = (GameLaunchWorkflowModel)workflowManager.GetModel(Guids.LaunchGameDialogWorkflow);
            launchDlg.SetGame(game);
            workflowManager.NavigatePush(Guids.LaunchGameDialogState);
        }

        public void ShowEmulatorContext(EmulatorViewModel emulator)
        {
            IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
            workflowManager.NavigatePushAsync(Guids.NewEmulatorState, new NavigationContextConfig());
        }

        #endregion

        #region Private Methods

        void loadStartupItems()
        {
            StartupState startupState = EmulatorsCore.Options.ReadOption(o => o.StartupState);
            if (startupState == StartupState.LASTUSED)
                startupState = EmulatorsCore.Options.ReadOption(o => o.LastStartupState);

            Guid stateId;
            NavigationData navigationData = new NavigationData();
            ItemsList items = new ItemsList();
            switch (startupState)
            {
                case StartupState.GROUPS:
                    stateId = Guids.WorkflowSatesGroups;
                    var groups = RomGroup.GetAll();
                    foreach (RomGroup g in groups)
                        items.Add(new GroupViewModel(g, this));
                    navigationData.DisplayName = "[Emulators.Groups]";
                    break;
                case StartupState.EMULATORS:
                default:
                    stateId = Guids.WorkflowSatesEmulators;
                    var emulators = Emulator.GetAll(true);
                    foreach (Emulator e in emulators)
                        items.Add(new EmulatorViewModel(e, this));
                    navigationData.DisplayName = "[Emulators.Emulators]";
                    break;
            }

            navigationData.ItemsList = items;
            PushState(stateId, navigationData);
        }

        void showGoodmergeDialog(GameViewModel selectedGame)
        {
            ItemsList items = new ItemsList();
            Game game = selectedGame.Game;
            int matchIndex;
            List<string> files = Extractor.Instance.ViewFiles(game, out matchIndex);
            if (files != null)
            {
                for (int x = 0; x < files.Count; x++)
                {
                    string file = files[x];
                    items.Add(new ListItem(Consts.KEY_NAME, file)
                    {
                        Selected = x == matchIndex,
                        Command = new MethodDelegateCommand(() =>
                        {
                            game.CurrentDisc.LaunchFile = file;
                            game.CurrentDisc.Commit();
                        })
                    });
                }
                if (items.Count > 0)
                {
                    var dialog = (ListDialogModel)ServiceRegistration.Get<IWorkflowManager>().GetModel(Guids.ListDialogWorkflow);
                    dialog.ShowDialog("[Emulators.SelectGoodmerge]", items);
                }
            }
        }

        #endregion

        #region IWorkflow

        public bool CanEnterState(MediaPortal.UI.Presentation.Workflow.NavigationContext oldContext, MediaPortal.UI.Presentation.Workflow.NavigationContext newContext)
        {
            return true;
        }

        public void ChangeModelContext(MediaPortal.UI.Presentation.Workflow.NavigationContext oldContext, MediaPortal.UI.Presentation.Workflow.NavigationContext newContext, bool push)
        {
            currentContext = newContext;
        }

        public void EnterModelContext(MediaPortal.UI.Presentation.Workflow.NavigationContext oldContext, MediaPortal.UI.Presentation.Workflow.NavigationContext newContext)
        {
            currentContext = newContext;
            if (newContext.WorkflowState.StateId == Guids.StartupState)
            {
                loadStartupItems();
            }
        }

        public void ExitModelContext(MediaPortal.UI.Presentation.Workflow.NavigationContext oldContext, MediaPortal.UI.Presentation.Workflow.NavigationContext newContext)
        {

        }

        public void Deactivate(MediaPortal.UI.Presentation.Workflow.NavigationContext oldContext, MediaPortal.UI.Presentation.Workflow.NavigationContext newContext)
        {

        }

        public void Reactivate(MediaPortal.UI.Presentation.Workflow.NavigationContext oldContext, MediaPortal.UI.Presentation.Workflow.NavigationContext newContext)
        {

        }

        public Guid ModelId
        {
            get { return Guids.WorkflowSatesMain; }
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
