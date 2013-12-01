using Emulators.Database;
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
        public EmulatorsWorkflowModel()
        {
            string dataPath = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\Emulators");
            string databasePath = Path.Combine(dataPath, "Emulators2_v1.db3");
            string optionsPath = Path.Combine(dataPath, "Emulators_2.xml");
            string groupsPath = Path.Combine(dataPath, "Emulators2Groups.xml");
            string defaultThumbs = @"C:\ProgramData\Team MediaPortal\MediaPortal\thumbs";

            Emulators2Settings.Instance.Init(new SQLProvider(databasePath), new MP2Logger(), optionsPath, groupsPath, defaultThumbs);

            _layoutTypeProperty = new WProperty(typeof(LayoutType), LayoutType.List);
            _focusedItemProperty = new WProperty(typeof(ItemViewModel), null);
        }

        ~EmulatorsWorkflowModel()
        {
            Options.Instance.Save();
        }

        void startUp()
        {
            Guid stateId;
            StartupState startupState = Options.Instance.GetStartupState();
            switch (startupState)
            {
                case StartupState.EMULATORS:
                default:
                    stateId = Guids.WorkflowSatesEmulators;
                    break;
            }
            IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
            workflowManager.NavigatePushAsync(stateId);
        }

        ItemsList emulatorsList;
        public ItemsList EmulatorsList
        {
            get
            {
                return emulatorsList;
            }
        }

        protected AbstractProperty _focusedItemProperty;
        public AbstractProperty FocusedItemProperty { get { return _focusedItemProperty; } }
        public ItemViewModel FocusedItem
        {
            get 
            {
                return (ItemViewModel)_focusedItemProperty.GetValue(); 
            }
            set 
            {
                if (value != null)
                {
                    _focusedItemProperty.SetValue(value);
                }
            }
        }

        public void SelectEmulator(EmulatorViewModel emulator)        
        {
            gamesList = new ItemsList();
            foreach (Game game in emulator.Emulator.Games)
            {
                Game lGame = game;
                gamesList.Add(new GameViewModel(game));
            }
            IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
            workflowManager.NavigatePushAsync(Guids.WorkflowSatesGames, new NavigationContextConfig());            
        }

        public void ShowEmulatorContext(EmulatorViewModel emulator)
        {
            IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
            workflowManager.NavigatePushAsync(Guids.NewEmulatorState, new NavigationContextConfig()); 
        }

        ItemsList gamesList;
        public ItemsList GamesList
        {
            get
            {
                return gamesList;
            }
        }

        public void SelectGame(GameViewModel game)
        {
            IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
            GameLaunchWorkflowModel launchDlg = (GameLaunchWorkflowModel)workflowManager.GetModel(Guids.LaunchGameDialogWorkflow);
            launchDlg.SetGame(game.Game);
            workflowManager.NavigatePushAsync(Guids.LaunchGameDialogState);
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

        #region Layout Properties

        protected AbstractProperty _layoutTypeProperty;
        public AbstractProperty LayoutTypeProperty { get { return _layoutTypeProperty; } }
        public LayoutType LayoutType
        {
            get { return (LayoutType)_layoutTypeProperty.GetValue(); }
            set { _layoutTypeProperty.SetValue(value); }
        }

        #endregion

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
            if (newContext.WorkflowState.StateId == Guids.StartupState)
            {
                emulatorsList = new ItemsList();
                foreach (Emulator emu in Emulator.GetAll(true))
                    emulatorsList.Add(new EmulatorViewModel(emu));
                startUp();
            }
        }

        public void ExitModelContext(MediaPortal.UI.Presentation.Workflow.NavigationContext oldContext, MediaPortal.UI.Presentation.Workflow.NavigationContext newContext)
        {
            
        }

        public Guid ModelId
        {
            get { return Guids.WorkflowSatesMain; }
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
