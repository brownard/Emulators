using Emulators.ImageHandlers;
using Emulators.Launcher;
using MediaPortal.GUI.Library;
using MediaPortal.InputDevices;
using MediaPortal.Player;
using MediaPortal.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Emulators.MediaPortal1
{
    class GUILauncher
    {
        Game game;
        EmulatorProfile emulatorProfile;
        GameLauncher launcher;
        public GUILauncher(Game game)
        {
            this.game = game;
            emulatorProfile = game.CurrentProfile;
        }

        public event EventHandler Started;
        protected virtual void OnStarted()
        {
            if (Started != null)
                Started(this, EventArgs.Empty);
        }

        public event EventHandler Stopped;
        protected virtual void OnStopped()
        {
            if (Stopped != null)
                Stopped(this, EventArgs.Empty);
        }

        public static void LaunchDocument(ThumbItem item)
        {
            string manualPath;
            using (ThumbGroup thumbGroup = new ThumbGroup(item))
                manualPath = thumbGroup.ManualPath;

            if (string.IsNullOrEmpty(manualPath))
                return;

            using (Process proc = new Process())
            {
                proc.StartInfo = new ProcessStartInfo();
                proc.StartInfo.FileName = manualPath;
                proc.Start();
            }
        }

        public void Launch()
        {
            stopMediaPlayback();
            checkController();
            OnStarted();
            launcher = new GameLauncher(game);
            BackgroundTaskHandler handler = new BackgroundTaskHandler();
            handler.ActionDelegate = () =>
            {
                launcher.ExtractionProgress += (s, e) =>
                    {
                        beginInvoke(() =>
                            {
                                handler.ExecuteProgressHandler(string.Format("Extracting {0}%", e.Percent), e.Percent);
                            });
                    };
                launcher.ExtractionFailed += launcher_ExtractionFailed;
                launcher.Starting += launcher_Starting;
                launcher.StartFailed += launcher_StartFailed;
                launcher.Exited += launcher_Exited;
                launcher.Launch();
            };
            MP1Utils.ShowProgressDialog(handler);
        }

        void launcher_ExtractionFailed(object sender, EventArgs e)
        {
            invoke(() =>
                {
                    MP1Utils.ShowMPDialog("Error extracting {0}", game.Title);
                });
        }

        void launcher_Starting(object sender, EventArgs e)
        {
            invoke(() =>
                {
                    if (emulatorProfile.MountImages)
                        mountImage(launcher.RomPath);
                    if (!MP1Utils.IsConfig && emulatorProfile.SuspendMP)
                        suspendMP(true);
                });
        }

        void launcher_Exited(object sender, EventArgs e)
        {
            if (!MP1Utils.IsConfig)
                game.UpdateAndSaveGamePlayInfo();
            if (!MP1Utils.IsConfig && emulatorProfile.SuspendMP && emulatorProfile.ResumeDelay > 0)
                System.Threading.Thread.Sleep(emulatorProfile.ResumeDelay);
            invoke(() =>
                {
                    onLauncherExit();
                });
        }

        void launcher_StartFailed(object sender, EventArgs e)
        {
            invoke(() =>
            {
                onLauncherExit();
                MP1Utils.ShowMPDialog("Error launching {0}", game.Title);
            });
        }

        void onLauncherExit()
        {
            if (!MP1Utils.IsConfig && emulatorProfile.SuspendMP)
                suspendMP(false);
            if (emulatorProfile.MountImages)
                unMountImage();
            OnStopped();
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
            if (!MP1Utils.IsConfig && EmulatorsCore.Options.ReadOption(o => o.StopMediaPlayback) && g_Player.Playing)
            {
                Logger.LogDebug("Stopping playing media...");
                g_Player.Stop();
            }
        }

        void checkController()
        {
            if (emulatorProfile.CheckController && !ControllerHandler.CheckControllerState())
                MP1Utils.ShowMPDialog(Translator.Instance.nocontrollerconnected);
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

        static void invoke(System.Action action)
        {
            GUIWindowManager.SendThreadCallbackAndWait((p1, p2, o) =>
                {
                    action();
                    return 0;
                }, 0, 0, null);
        }

        static void beginInvoke(System.Action action)
        {
            GUIWindowManager.SendThreadCallback((p1, p2, o) =>
            {
                action();
                return 0;
            }, 0, 0, null);
        }
    }
}
