using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPortal.GUI.Library;
using Emulators.Import;
using Emulators.Database;

namespace Emulators.MediaPortal1
{
    partial class GUIPresenter
    {
        object importControllerSync = new object();
        Importer importer = null;
        bool? autoimport = null;
        volatile bool restarting = false;

        void initImporter()
        {
            GUIPropertyManager.SetProperty("#Emulators2.Importer.working", "no");
            EmulatorsCore.Options.EnterReadLock();
            if (EmulatorsCore.Options.AutoImportGames)
                autoimport = true;
            else if (EmulatorsCore.Options.AutoRefreshGames)
                autoimport = false;
            else
                autoimport = null;
            EmulatorsCore.Options.ExitReadLock();

            importer = new Importer(true, autoimport == false);
            importer.ImportStatusChanged += importerStatusChangedHandler;
            importer.RomStatusChanged += romStatusChangedHandler;
            EmulatorsCore.Database.OnItemDeleting += Database_OnItemDeleting;

            //pause importer during game execution
            LaunchHandler.Instance.StatusChanged += launch_StatusChanged;
            if (autoimport != null)
                importer.Start();
        }

        void Database_OnItemDeleting(DBItem changedItem)
        {
            Game game = changedItem as Game;
            if (game != null && importer != null)
                importer.Remove(game.Id);
        }
        
        void resumeImporter()
        {
            if (importer == null)
                return;

            importer.UnPause();
        }

        public void RestartImporter()
        {
            if (restarting)
                return;

            lock (importControllerSync)
            {                
                importer.JustRefresh = false;
                restarting = true;
                importer.Restart();
            }
        }

        public void AddToImporter(Game game)
        {
            if (game != null)
                importer.AddGames(new Game[] { game });
        }

        void importerStatusChangedHandler(object sender, ImportStatusEventArgs e)
        {
            bool working = false;
            switch (e.Action)
            {
                case ImportAction.ImportStarted:
                    restarting = false;
                    working = true;
                    break;
                case ImportAction.ImportResumed:
                    working = true;
                    break;
                case ImportAction.NewFilesFound:
                    working = true;
                    UpdateFacade();
                    break;
                case ImportAction.ImportFinished:
                    UpdateFacade();
                    break;
                case ImportAction.ImportPaused:
                    break;
                default:
                    return;
            }
            GUIPropertyManager.SetProperty("#Emulators2.Importer.working", working ? "yes" : "no");
            Logger.LogDebug("Importer action: {0}", e.Action.ToString());
        }

        void romStatusChangedHandler(object sender, RomStatusEventArgs e)
        {
            if (e.Status == RomMatchStatus.Committed)
            {
                Game game = EmulatorsCore.Database.Get<Game>(e.RomMatch.ID);
                Logger.LogDebug("Importer action: {0} updated", game.Title);
                UpdateGame(game);
            }
        }

        void launch_StatusChanged(bool isRunning)
        {
            lock (importControllerSync)
            {
                if (importer == null)
                    return;
                //toggle pause state
                if (isRunning)
                    importer.Pause();
                else
                    importer.UnPause();
            }
        }
    }
}
