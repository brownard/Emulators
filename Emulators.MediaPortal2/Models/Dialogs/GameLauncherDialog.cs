using Emulators.Launcher;
using Emulators.MediaPortal2.Models;
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

namespace Emulators.MediaPortal2.Models.Dialogs
{
    class GameLauncherDialog : ProgressDialogModel
    {
        Game game = null;
        GameLauncher launcher;

        public static void Launch(Game game)
        {
            IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
            GameLauncherDialog instance = (GameLauncherDialog)workflowManager.GetModel(Guids.GameLauncherDialogModel);
            instance.Game = game;
            workflowManager.NavigatePushAsync(Guids.GameLauncherDialogState);
        }

        public Game Game
        {
            get { return game; }
            set { game = value; }
        }

        public override Guid ModelId { get { return Guids.GameLauncherDialogModel; } }

        protected override void DoTask()
        {          
            SetProgress("Launching " + game.Title, 0);
            launcher = new GameLauncher(game);
            launcher.ExtractionProgress += (s, e) => SetProgress(string.Format("Extracting {0}%", e.Percent), e.Percent);
            launcher.Starting += (s, e) => SetProgress("Launching...", 50);
            launcher.Exited += launcher_Exited;
            launcher.Launch();
        }

        void launcher_Exited(object sender, EventArgs e)
        {
            ((GameLauncher)sender).Game.UpdateAndSaveGamePlayInfo();
        }
    }
}
