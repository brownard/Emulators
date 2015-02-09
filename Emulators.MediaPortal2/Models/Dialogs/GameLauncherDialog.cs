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
    class GameLauncherDialog
    {
        const string HEADER = "[Emulators.Dialogs.LaunchGame]";
        Game game = null;
        GameLauncher launcher;

        public GameLauncherDialog(Game game)
        {
            this.game = game;
        }

        public void Launch()
        {
            ProgressDialogModel.ShowDialog(HEADER, taskDelegate);
        }

        void taskDelegate(ProgressDialogModel progressDlg)
        {
            progressDlg.SetProgress("Launching " + game.Title, 0);
            launcher = new GameLauncher(game);
            launcher.ExtractionProgress += (s, e) => progressDlg.SetProgress(string.Format("Extracting {0}%", e.Percent), e.Percent);
            launcher.Starting += (s, e) => progressDlg.SetProgress("Launching...", 50);
            launcher.Exited += launcher_Exited;
            launcher.Launch();
        }

        void launcher_Exited(object sender, EventArgs e)
        {
            ((GameLauncher)sender).Game.UpdateAndSaveGamePlayInfo();
        }
    }
}
