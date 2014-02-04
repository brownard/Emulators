using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using MediaPortal.GUI.Library;
using MediaPortal.Dialogs;
using Emulators.MediaPortal1;
using System.IO;
using MediaPortal.Util;
using MediaPortal.InputDevices;
using System.Diagnostics;

namespace Emulators
{
    public delegate void LaunchStatusChangedEventHandler(bool isRunning);
    class LaunchHandler : IDisposable
    {
        #region Singleton

        protected static object instanceSync = new object();
        protected static LaunchHandler instance = null;
        public static LaunchHandler Instance
        {
            get
            {
                if (instance == null)
                    lock (instanceSync)
                        if (instance == null)
                            instance = new LaunchHandler();
                return instance;
            }
        }

        #endregion
        
        public static void LaunchDocument(ThumbItem item)
        {
            string manualPath = null;
            using (ThumbGroup thumbGroup = new ThumbGroup(item))
                manualPath = thumbGroup.ManualPath;

            if (string.IsNullOrEmpty(manualPath))
                return;

            //Execute
            using (Process proc = new Process())
            {
                proc.StartInfo = new ProcessStartInfo();
                proc.StartInfo.FileName = manualPath;
                proc.Start();
            }
        }

        object launchSync = new object();
        ExecutorItem launcher = null;
        public event LaunchStatusChangedEventHandler StatusChanged;

        public void StartLaunch(Game game)
        {
            if (game == null)
                return;

            EmulatorProfile profile = game.CurrentProfile;
            if (profile == null)
            {
                Logger.LogError("No profile found for '{0}'", game.Title);
                return;
            }

            bool isConfig = EmulatorsSettings.Instance.IsConfig;
            BackgroundTaskHandler handler = new BackgroundTaskHandler();
            handler.ActionDelegate = () =>
            {
                lock (launchSync)
                {
                    string errorStr = null;
                    string path = null;

                    if (game.IsGoodmerge)
                    {
                        try
                        {
                            path = Extractor.Instance.ExtractGame(game, profile, (l, p) => handler.ExecuteProgressHandler(l, p));
                        }
                        catch (ArchiveException)
                        {
                            errorStr = Translator.Instance.goodmergearchiveerror;
                        }
                        catch (ArchiveEmptyException)
                        {
                            errorStr = Translator.Instance.goodmergeempty;
                        }
                        catch (ExtractException)
                        {
                            errorStr = Translator.Instance.goodmergeextracterror;
                        }
                    }
                    else
                        path = game.CurrentDisc.Path;

                    GUIGraphicsContext.form.Invoke(new System.Windows.Forms.MethodInvoker(() =>
                    {
                        if (path != null)
                        {
                            try
                            {
                                handler.ExecuteProgressHandler("Launching " + game.Title, 50);
                                if (launcher != null)
                                    launcher.Dispose();
                                launcher = createLauncher(path, profile, game.ParentEmulator.IsPc());

                                if (!isConfig)
                                    stopMediaPlayback();

                                if (launcher.CheckController && !ControllerHandler.CheckControllerState())
                                    MP1Utils.ShowMPDialog(Translator.Instance.nocontrollerconnected);
                                
                                if (StatusChanged != null)
                                    StatusChanged(true);

                                launcher.Launch();
                                if (!isConfig)
                                    game.UpdateAndSaveGamePlayInfo();
                            }
                            catch (LaunchException ex)
                            {
                                Logger.LogError(ex.Message);
                                errorStr = ex.Message;
                            }
                        }

                        if (errorStr != null)
                            MP1Utils.ShowMPDialog("Error\r\n{0}", errorStr);
                    }));
                }
            };

            MP1Utils.ShowProgressDialog(handler);
        }
        
        ExecutorItem createLauncher(string path, EmulatorProfile profile, bool isPc)
        {
            ExecutorItem launcher = new ExecutorItem(isPc);
            launcher.Arguments = profile.Arguments;
            launcher.Suspend = !EmulatorsSettings.Instance.IsConfig && profile.SuspendMP == true;
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
                launcher.Mount = profile.MountImages && DaemonTools.IsImageFile(Path.GetExtension(path));
                launcher.ShouldReplaceWildcards = !launcher.Mount;
                launcher.UseQuotes = profile.UseQuotes == true;
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

            launcher.OnStarting += launcher_OnStarting;
            launcher.OnStartFailed += launcher_OnExited;
            launcher.OnExited += launcher_OnExited;

            return launcher;
        }

        void launcher_OnStarting(object sender, EventArgs e)
        {
            ExecutorItem launcher = (ExecutorItem)sender;
            if (launcher.Mount)
                mountImage(launcher.RomPath);
            if (launcher.Suspend)
                suspendMP(true);
        }

        void launcher_OnExited(object sender, EventArgs e)
        {
            ExecutorItem launcher = (ExecutorItem)sender;
            if (launcher.Mount)
                unMountImage();
            if (launcher.Suspend)
            {
                if (launcher.ResumeDelay > 0)
                    System.Threading.Thread.Sleep(launcher.ResumeDelay);
                suspendMP(false);
            }

            if (StatusChanged != null)
                StatusChanged(false);
        }

        void suspendMP(bool suspend)
        {
            if (suspend) //suspend and hide MediaPortal
            {
                Logger.LogDebug("Suspending MediaPortal...");
                // disable mediaportal input devices
                InputDevices.Stop();

                // hide mediaportal and suspend rendering to save resources for the pc game
                GUIGraphicsContext.BlankScreen = true;
                GUIGraphicsContext.form.Hide();
                GUIGraphicsContext.CurrentState = GUIGraphicsContext.State.SUSPENDING;
            }
            else //resume Mediaportal
            {
                Logger.LogDebug("Resuming MediaPortal...");

                InputDevices.Init();
                // Resume Mediaportal rendering
                GUIGraphicsContext.BlankScreen = false;
                GUIGraphicsContext.form.Show();
                GUIGraphicsContext.ResetLastActivity();
                GUIMessage msg = new GUIMessage(GUIMessage.MessageType.GUI_MSG_GETFOCUS, 0, 0, 0, 0, 0, null);
                GUIWindowManager.SendThreadMessage(msg);
                GUIGraphicsContext.CurrentState = GUIGraphicsContext.State.RUNNING;
            }
        }

        void stopMediaPlayback()
        {
            if (Options.Instance.GetBoolOption("stopmediaplayback") && MediaPortal.Player.g_Player.Playing)
            {
                Logger.LogDebug("Stopping playing media...");
                MediaPortal.Player.g_Player.Stop();
            }
        }

        void mountImage(string path)
        {
            Logger.LogInfo("Attempting to mount image file '{0}'", path);
            if (!DaemonTools.IsEnabled)
            {
                Logger.LogWarn("Attempt to mount image file '{0}' failed, Mediaportal's Virtual Drive is not enabled", path);
                return;
            }
            string drive;
            DaemonTools.UnMount();
            if (!DaemonTools.Mount(path, out drive))
                Logger.LogWarn("Attempt to mount image file '{0}' failed", path);
        }

        void unMountImage()
        {
            if (DaemonTools.IsEnabled)
                DaemonTools.UnMount();
        }

        public void Dispose()
        {
            if (launcher != null)
            {
                launcher.Dispose();
                launcher = null;
            }
        }
    }
}
