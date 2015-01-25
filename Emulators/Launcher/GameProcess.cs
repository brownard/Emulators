using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Emulators.Launcher
{
    public class GameProcess : IDisposable
    {
        EmulatorProfile emulatorProfile;
        GamePath gamePath;
        Process process;
        List<int> ignoredProcessIds;
        KeyboardHook keyHook;
        int mappedKeyData;

        public event EventHandler Starting;
        protected virtual void OnStarting()
        {
            if (Starting != null)
                Starting(this, EventArgs.Empty);
        }
        public event EventHandler Started;
        protected virtual void OnStarted()
        {
            if (Started != null)
                Started(this, EventArgs.Empty);
        }
        public event EventHandler StartFailed;
        protected virtual void OnStartFailed()
        {
            if (StartFailed != null)
                StartFailed(this, EventArgs.Empty);
        }
        public event EventHandler Exited;
        protected virtual void OnExited()
        {
            if (Exited != null)
                Exited(this, EventArgs.Empty);
        }

        public GameProcess(string path, EmulatorProfile emulatorProfile, bool isPC)
        {
            this.emulatorProfile = emulatorProfile;
            if (isPC)
                gamePath = GamePathBuilder.CreatePCPath(path, emulatorProfile.Arguments);
            else
                gamePath = GamePathBuilder.CreateRomPath(emulatorProfile.EmulatorPath, emulatorProfile.Arguments, path, !emulatorProfile.MountImages, emulatorProfile.UseQuotes);

            if (!string.IsNullOrEmpty(emulatorProfile.WorkingDirectory))
                gamePath.WorkingDirectory = emulatorProfile.WorkingDirectory;

            if (emulatorProfile.StopEmulationOnKey == true || (emulatorProfile.StopEmulationOnKey == null && EmulatorsCore.Options.ReadOption(o => o.StopOnMappedKey)))
                mappedKeyData = EmulatorsCore.Options.ReadOption(o => o.MappedKey);
        }

        public void Start()
        {
            Logger.LogInfo("Starting game: Path: '{0}', Arguments '{1}', Working Directory '{2}'", gamePath.Path, gamePath.Arguments, gamePath.WorkingDirectory);

            initProcess();
            OnStarting();
            runCommand(emulatorProfile.PreCommand, emulatorProfile.PreCommandWaitForExit, emulatorProfile.PreCommandShowWindow);

            if (!string.IsNullOrEmpty(emulatorProfile.LaunchedExe))
                ignoredProcessIds = Process.GetProcessesByName(emulatorProfile.LaunchedExe).Select(p => p.Id).ToList();

            if (!tryStartProcess())
            {
                OnStartFailed();
                return;
            }

            if (mappedKeyData > 0)
            {
                Logger.LogDebug("Initialising keyboard hook, Process Id: {0}", process.Id);
                keyHook = new KeyboardHook(process.Id, onMappedKey); //setup hook and attach to emu process
            }

            OnStarted();
        }

        void initProcess()
        {
            process = new Process();
            process.StartInfo = new ProcessStartInfo(gamePath.Path, gamePath.Arguments);
            process.StartInfo.WorkingDirectory = gamePath.WorkingDirectory;
            process.EnableRaisingEvents = true;
            process.Exited += process_Exited;
        }

        bool tryStartProcess()
        {
            try
            {
                return process.Start();
            }
            catch (Exception ex)
            {
                Logger.LogError("GameLauncher: Error starting process {0}, {1} - {2}", process.StartInfo.FileName, ex, ex.Message);
            }
            return false;
        }

        void process_Exited(object sender, EventArgs e)
        {
            if (ignoredProcessIds != null)
                waitForLaunchedProcess();
            runCommand(emulatorProfile.PostCommand, emulatorProfile.PostCommandWaitForExit, emulatorProfile.PostCommandShowWindow);
            OnExited();
        }

        void waitForLaunchedProcess()
        {
            foreach (Process proc in System.Diagnostics.Process.GetProcessesByName(emulatorProfile.LaunchedExe))
            {
                if (!ignoredProcessIds.Contains(proc.Id))
                {
                    proc.WaitForExit();
                    break;
                }
            }
        }

        void onMappedKey(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if ((int)e.KeyData != mappedKeyData)
                return;

            Logger.LogDebug("Keyboard hook - Mapped key pressed, stopping emulation");
            e.Handled = true;
            e.SuppressKeyPress = true;

            int Msg;
            uint wParam;
            if (emulatorProfile.EscapeToExit)
            {
                //set message to Esc key press
                Msg = KeyboardHook.WM_KEYDOWN;
                wParam = KeyboardHook.VK_ESCAPE;
            }
            else
            {
                //Set message to window close
                Msg = KeyboardHook.WM_CLOSE;
                wParam = 0;
            }

            try
            {
                IntPtr wH = process.MainWindowHandle;
                if (wH != IntPtr.Zero)
                    KeyboardHook.PostMessage(wH, Msg, wParam, 0);
            }
            catch (Exception ex)
            {
                Logger.LogError("GameLauncher: Error sending close message to emulator - {0}", ex.Message);
            }
        }

        static void runCommand(string command, bool waitForExit, bool showWindow)
        {
            if (string.IsNullOrEmpty(command))
                return;
            using (Process cmd = new Process())
            {
                cmd.StartInfo = new ProcessStartInfo("cmd.exe", string.Format("/C {0}", command));
                cmd.StartInfo.WindowStyle = showWindow ? ProcessWindowStyle.Normal : ProcessWindowStyle.Hidden;
                try
                {
                    cmd.Start();
                    if (waitForExit)
                        cmd.WaitForExit();
                }
                catch (Exception ex)
                {
                    Logger.LogError("GameLauncher: Error running command line '{0}' - {1}", command, ex.Message);
                }
            }
        }

        public void Dispose()
        {
            if (process != null)
            {
                process.Dispose();
                process = null;
            }
            if (keyHook != null)
            {
                keyHook.Dispose();
                keyHook = null;
            }
        }
    }
}
