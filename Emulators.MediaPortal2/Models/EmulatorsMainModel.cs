using Emulators.Database;
using Emulators.Import;
using Emulators.Launcher;
using Emulators.MediaPortal2.Models.Dialogs;
using Emulators.MediaPortal2.Navigation;
using MediaPortal.Common;
using MediaPortal.Common.Commands;
using MediaPortal.Common.General;
using MediaPortal.Common.PathManager;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.SkinEngine.MpfElements;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.MediaPortal2.Models
{
    public class EmulatorsMainModel : IWorkflowModel
    {
        #region Static Methods

        public static NavigationData GetNavigationData(NavigationContext context)
        {
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
            workflowManager.NavigatePushTransientAsync(newState, GetContextConfig(navigationData));
        }

        public static EmulatorsMainModel Instance()
        {
            IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
            return (EmulatorsMainModel)workflowManager.GetModel(Guids.EmulatorsMainModel);
        }

        #endregion

        #region Members

        protected AbstractProperty _focusedItemProperty;
        protected AbstractProperty _layoutTypeProperty;
        protected AbstractProperty _headerProperty;
        protected AbstractProperty _currentFanartProperty;

        NavigationData navigationData;

        #endregion

        #region Ctor/Dtor

        public EmulatorsMainModel()
        {
            _layoutTypeProperty = new WProperty(typeof(LayoutType), LayoutType.List);
            _focusedItemProperty = new WProperty(typeof(ListItem), null);
            _headerProperty = new WProperty(typeof(string));
            _currentFanartProperty = new WProperty(typeof(string));
        }

        #endregion

        #region Public Properties

        public NavigationData NavigationData
        {
            get { return navigationData; }
        }

        public ItemsList Items
        {
            get;
            private set;
        }

        public AbstractProperty HeaderProperty { get { return _headerProperty; } }
        public string Header
        {
            get { return (string)_headerProperty.GetValue(); }
            set { _headerProperty.SetValue(value); }
        }

        public AbstractProperty FocusedItemProperty { get { return _focusedItemProperty; } }
        public ListItem FocusedItem
        {
            get { return (ItemViewModel)_focusedItemProperty.GetValue(); }
            set { _focusedItemProperty.SetValue(value); }
        }

        public AbstractProperty CurrentFanartProperty { get { return _currentFanartProperty; } }
        public string CurrentFanart
        {
            get { return (string)_currentFanartProperty.GetValue(); }
            set { _currentFanartProperty.SetValue(value); }
        }

        public AbstractProperty LayoutTypeProperty { get { return _layoutTypeProperty; } }
        public LayoutType LayoutType
        {
            get { return (LayoutType)_layoutTypeProperty.GetValue(); }
            set { _layoutTypeProperty.SetValue(value); }
        }

        StartupState startupState = StartupState.LASTUSED;
        public StartupState StartupState
        {
            get { return startupState; }
            set
            {
                if (value != startupState)
                {
                    startupState = value;
                    var workflowManager = ServiceRegistration.Get<IWorkflowManager>();
                    var currentContext = workflowManager.CurrentNavigationContext;
                    if (currentContext.WorkflowState.StateId == Guids.ViewItemsState)
                        updateState(currentContext);
                    else if (currentContext.WorkflowModelId == Guids.EmulatorsMainModel)
                        workflowManager.NavigatePopToState(Guids.ViewItemsState, false);
                }
            }
        }

        #endregion

        #region Public Methods

        public void SetFocusedItem(object sender, SelectionChangedEventArgs e)
        {
            ItemViewModel item = e.FirstAddedItem as ItemViewModel;
            if (item != null)
            {
                FocusedItem = item;
                CurrentFanart = item.Fanart;
            }
        }

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
            List<ListItem> items = emulator.Games.Select(g => (ListItem)new GameViewModel(g, this)).ToList();
            NavigationData navigationData = new NavigationData() { DisplayName = emulator.Title, ItemsList = items };
            PushTransientState("emuGames", navigationData.DisplayName, navigationData);
        }

        public void GroupSelected(RomGroup group)
        {
            group.Refresh();
            List<ListItem> items = new List<ListItem>();
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
            GameLauncherDialog.Launch(game);
        }

        public void ShowEmulatorContext(EmulatorViewModel emulator)
        {
            IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
            workflowManager.NavigatePushAsync(Guids.NewEmulatorState, new NavigationContextConfig());
        }

        #endregion

        #region Private Methods

        void updateState(NavigationContext context)
        {
            bool updateList = false;
            navigationData = GetNavigationData(context);
            if (context.WorkflowState.StateId == Guids.ViewItemsState && (navigationData == null || navigationData.StartupState != startupState))
            {
                updateList = true;
                navigationData = getStartupNavigationData();
                context.SetContextVariable(NavigationData.NAVIGATION_DATA, navigationData);
            }

            if (navigationData != null)
            {
                Header = navigationData.DisplayName;
                ItemsList items = Items;
                if (items != null && updateList)
                {
                    items.Clear();
                    foreach (ListItem item in navigationData.ItemsList)
                        items.Add(item);
                    items.FireChange();
                }
                else
                {
                    items = new ItemsList();
                    foreach (ListItem item in navigationData.ItemsList)
                        items.Add(item);
                    Items = items;
                }
            }
        }

        NavigationData getStartupNavigationData()
        {
            NavigationData navigationData = new NavigationData();
            List<ListItem> items;
            switch (startupState)
            {
                case StartupState.GROUPS:
                    navigationData.DisplayName = "[Emulators.Groups]";
                    var groups = RomGroup.GetAll();
                    items = groups.Select(g => (ListItem)new GroupViewModel(g, this)).ToList();
                    break;
                case StartupState.PCGAMES:
                    navigationData.DisplayName = "[Emulators.PCGames]";
                    var games = Emulator.GetPC().Games;
                    items = games.Select(g => (ListItem)new GameViewModel(g, this)).ToList();
                    break;
                case StartupState.FAVOURITES:
                    navigationData.DisplayName = "[Emulators.Favourites]";
                    BaseCriteria favCriteria = new BaseCriteria(DBField.GetField(typeof(Game), "Favourite"), "=", true);
                    var favourites = EmulatorsCore.Database.Get<Game>(favCriteria);
                    items = favourites.Select(g => (ListItem)new GameViewModel(g, this)).ToList();
                    break;
                case StartupState.EMULATORS:
                default:
                    navigationData.DisplayName = "[Emulators.Emulators]";
                    var emulators = Emulator.GetAll(true);
                    items = emulators.Select(e => (ListItem)new EmulatorViewModel(e, this)).ToList();
                    break;
            }
            navigationData.StartupState = startupState;
            navigationData.ItemsList = items;
            return navigationData;
        }

        void showGoodmergeDialog(GameViewModel selectedGame)
        {
            ItemsList items = new ItemsList();
            Game game = selectedGame.Game;
            List<string> files = SharpCompressExtractor.ViewFiles(game.CurrentDisc.Path);
            int matchIndex = GoodmergeHandler.GetFileIndex(game.CurrentDisc.LaunchFile, files, game.CurrentProfile.GetGoodmergeTags());
            if (files == null || files.Count == 0)
                return;

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
            ListDialogModel.Instance().ShowDialog("[Emulators.SelectGoodmerge]", items);
        }

        #endregion

        #region IWorkflow

        public bool CanEnterState(MediaPortal.UI.Presentation.Workflow.NavigationContext oldContext, MediaPortal.UI.Presentation.Workflow.NavigationContext newContext)
        {
            return true;
        }

        public void ChangeModelContext(MediaPortal.UI.Presentation.Workflow.NavigationContext oldContext, MediaPortal.UI.Presentation.Workflow.NavigationContext newContext, bool push)
        {
            updateState(newContext);
        }

        public void EnterModelContext(MediaPortal.UI.Presentation.Workflow.NavigationContext oldContext, MediaPortal.UI.Presentation.Workflow.NavigationContext newContext)
        {
            if (startupState == StartupState.LASTUSED)
            {
                startupState = EmulatorsCore.Options.ReadOption(o => o.StartupState);
                if (startupState == StartupState.LASTUSED)
                    startupState = EmulatorsCore.Options.ReadOption(o => o.LastStartupState);
            }
            updateState(newContext);
        }

        public void ExitModelContext(MediaPortal.UI.Presentation.Workflow.NavigationContext oldContext, MediaPortal.UI.Presentation.Workflow.NavigationContext newContext)
        {
            EmulatorsCore.Options.WriteOption(o => o.LastStartupState = startupState);
        }

        public void Deactivate(MediaPortal.UI.Presentation.Workflow.NavigationContext oldContext, MediaPortal.UI.Presentation.Workflow.NavigationContext newContext)
        {

        }

        public void Reactivate(MediaPortal.UI.Presentation.Workflow.NavigationContext oldContext, MediaPortal.UI.Presentation.Workflow.NavigationContext newContext)
        {

        }

        public Guid ModelId
        {
            get { return Guids.EmulatorsMainModel; }
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
