using Emulators.Launcher;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Threading;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.SkinResources;
using MediaPortal.UI.Presentation.Workflow;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Emulators.MediaPortal2
{
    class GameLaunchWorkflowModel : IWorkflowModel
    {
        public GameLaunchWorkflowModel()
        {
            _progressProperty = new WProperty(typeof(int), 0);
            _infoProperty = new WProperty(typeof(string), null);
        }

        Game game = null;
        GameLauncher launcher;
        IWork currentBackgroundTask = null;

        public void SetGame(Game game)
        {
            this.game = game;
        }

        void startLaunch(NavigationContext context)
        {
            if (game == null)
                return;

            launcher = new GameLauncher(game);
            currentBackgroundTask = ServiceRegistration.Get<IThreadPool>().Add(() =>
            {
                setProgress("Launching " + game.Title, 0);
                launcher.ExtractionProgress += (s, e) =>
                {
                    setProgress(string.Format("Extracting {0}%", e.Percent), e.Percent);
                };
                launcher.Starting += launcher_Starting;
                launcher.Exited += launcher_Exited;
                launcher.Launch();
            }, (args) =>
            {
                currentBackgroundTask = null;
                var screenMgr = ServiceRegistration.Get<IScreenManager>();
                if (screenMgr.TopmostDialogInstanceId == context.DialogInstanceId)
                    screenMgr.CloseTopmostDialog();
            });
        }

        void launcher_Starting(object sender, EventArgs e)
        {
            setProgress("Launching...", 50);
        }

        void launcher_Exited(object sender, EventArgs e)
        {
            ((GameLauncher)sender).Game.UpdateAndSaveGamePlayInfo();
        }

        void setProgress(string l, int p)
        {
            Info = l;
            Progress = p;
        }

        protected AbstractProperty _progressProperty;
        public AbstractProperty ProgressProperty { get { return _progressProperty; } }
        public int Progress
        {
            get { return (int)_progressProperty.GetValue(); }
            set { _progressProperty.SetValue(value); }
        }

        protected AbstractProperty _infoProperty;
        public AbstractProperty InfoProperty { get { return _infoProperty; } }
        public string Info
        {
            get { return (string)_infoProperty.GetValue(); }
            set { _infoProperty.SetValue(value); }
        }

        #region Workflow
        public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
        {
            return true;
        }

        public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
        {

        }

        public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
        {

        }

        public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
        {
            startLaunch(newContext);
        }

        public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
        {
            while (currentBackgroundTask != null)
                System.Threading.Thread.Sleep(100);
        }

        public Guid ModelId
        {
            get { return Guids.LaunchGameDialogWorkflow; }
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
