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
        ExecutorItem launchItem = null;
        object syncRoot = new object();
        IWork currentBackgroundTask = null;
        public void SetGame(Game game)
        {
            this.game = game;
        }

        void startLaunch(NavigationContext context)
        {
            if (launchItem != null)
            {
                launchItem.Dispose();
                launchItem = null;
            }
            if (game == null)
                return;
            if (game.CurrentProfile == null)
            {
                Logger.LogError("No profile found for '{0}'", game.Title);
                return;
            }

            currentBackgroundTask = ServiceRegistration.Get<IThreadPool>().Add(() =>
            {
                setProgress("Launching " + game.Title, 0);
                //string errorStr = null;
                string path = null;
                EmulatorProfile profile = game.CurrentProfile;
                if (game.IsGoodmerge)
                {
                    try
                    {
                        path = Extractor.Instance.ExtractGame(game, profile, setProgress);
                    }
                    catch (ArchiveException)
                    {
                        //errorStr = Translator.Instance.goodmergearchiveerror;
                    }
                    catch (ArchiveEmptyException)
                    {
                        //errorStr = Translator.Instance.goodmergeempty;
                    }
                    catch (ExtractException)
                    {
                        //errorStr = Translator.Instance.goodmergeextracterror;
                    }
                }
                else
                {
                    path = game.CurrentDisc.Path;
                }

                if (path != null)
                {
                    setProgress("Launching " + game.Title, 50);
                    launchItem = createLauncher(path, profile, game.ParentEmulator.IsPc());
                    launchItem.Launch();
                    game.PlayCount++;
                    game.Latestplay = DateTime.Now;
                    game.Commit();
                }

            }, (args) => 
            {
                //lock (syncRoot)
                currentBackgroundTask = null; 
                var screenMgr = ServiceRegistration.Get<IScreenManager>();
                if (screenMgr.TopmostDialogInstanceId == context.DialogInstanceId)
                    screenMgr.CloseTopmostDialog();
            });
        }

        void setProgress(string l, int p)
        {
            Info = l;
            Progress = p;
        }

        ExecutorItem createLauncher(string path, EmulatorProfile profile, bool isPc)
        {
            ExecutorItem launcher = new ExecutorItem(isPc);
            launcher.Arguments = profile.Arguments;
            launcher.Suspend = !EmulatorsSettings.Instance.IsConfig && profile.SuspendMP;
            if (launcher.Suspend && profile.DelayResume && profile.ResumeDelay > 0)
                launcher.ResumeDelay = profile.ResumeDelay;

            if (isPc)
            {
                launcher.Path = path;
                if (!string.IsNullOrEmpty(profile.LaunchedExe))
                {
                    try { launcher.LaunchedExe = Path.GetFileNameWithoutExtension(profile.LaunchedExe); }
                    catch { launcher.LaunchedExe = profile.LaunchedExe; }
                }
            }
            else
            {
                launcher.Path = profile.EmulatorPath;
                launcher.RomPath = path;
                launcher.Mount = profile.MountImages; // && DaemonTools.IsImageFile(Path.GetExtension(path));
                launcher.ShouldReplaceWildcards = !launcher.Mount;
                launcher.UseQuotes = profile.UseQuotes;
                launcher.CheckController = profile.CheckController;
                bool mapKey;
                if (profile.StopEmulationOnKey.HasValue)
                    mapKey = profile.StopEmulationOnKey.Value;
                else
                    mapKey = Options.Instance.GetBoolOption("domap");
                if (mapKey)
                {
                    launcher.MappedExitKeyData = Options.Instance.GetIntOption("mappedkeydata");
                    launcher.EscapeToExit = profile.EscapeToExit;
                }
            }
            launcher.PreCommand = new LaunchCommand() { Command = profile.PreCommand, WaitForExit = profile.PreCommandWaitForExit, ShowWindow = profile.PreCommandShowWindow };
            launcher.PostCommand = new LaunchCommand() { Command = profile.PostCommand, WaitForExit = profile.PostCommandWaitForExit, ShowWindow = profile.PostCommandShowWindow };

            //launcher.OnStarting += launcher_OnStarting;
            //launcher.OnStartFailed += launcher_OnExited;
            //launcher.OnExited += launcher_OnExited;

            return launcher;
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
