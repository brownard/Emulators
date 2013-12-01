using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace Emulators
{
    public delegate void ExecutorLaunchHandler(object sender, EventArgs e);

    public class ExecutorItem : IDisposable
    {
        Process process;
        public event ExecutorLaunchHandler OnStarting;
        public event ExecutorLaunchHandler OnStarted;
        public event ExecutorLaunchHandler OnStartFailed;
        public event ExecutorLaunchHandler OnExited;

        List<int> runningProcessIds = null;
        KeyboardHook keyHook = null;

        bool isPC = false;
        public bool IsPC { get { return isPC; } }
        string path = null;
        public string Path { get { return path; } set { path = value; } }
        string arguments = null;
        public string Arguments { get { return arguments; } set { arguments = value; } }
        string workingDirectory = null;
        public string WorkingDirectory { get { return workingDirectory; } set { path = workingDirectory; } }
        string romPath = null;
        public string RomPath { get { return romPath; } set { romPath = value; } }
        bool shouldReplaceWildcards = false;
        public bool ShouldReplaceWildcards { get { return shouldReplaceWildcards; } set { shouldReplaceWildcards = value; } }
        bool useQuotes = false;

        public bool UseQuotes { get { return useQuotes; } set { useQuotes = value; } }
        public bool Mount { get; set; }
        public bool Suspend { get; set; }
        public int ResumeDelay { get; set; }
        public string LaunchedExe { get; set; }
        public LaunchCommand PreCommand { get; set; }
        public LaunchCommand PostCommand { get; set; }
        public bool CheckController { get; set; }
        public bool StopEmulationOnKey { get; set; }
        public int MappedExitKeyData { get; set; }
        public bool EscapeToExit { get; set; }

        public ExecutorItem(bool isPC)
        {
            this.isPC = isPC;
        }

        public void Launch()
        {
            init();
            Logger.LogInfo("Launching game: Path: '{0}', Arguments '{1}', Working Directory '{2}'", path, arguments, workingDirectory);
            process = new Process();
            process.StartInfo = new ProcessStartInfo(path, arguments) { WorkingDirectory = workingDirectory };
            process.EnableRaisingEvents = true;
            process.Exited += (s, e) => 
            {
                waitForLaunchedProcess();
                if (PostCommand != null)
                    PostCommand.Run();
                if (OnExited != null)
                    OnExited(this, new EventArgs());
                Dispose();
            };

            if (OnStarting != null)
                OnStarting(this, new EventArgs());

            if (PreCommand != null)
                PreCommand.Run();
            
            if (!string.IsNullOrEmpty(LaunchedExe))
            {
                runningProcessIds = new List<int>();
                foreach (Process p in Process.GetProcessesByName(LaunchedExe))
                    runningProcessIds.Add(p.Id);
            }

            bool started = false;
            try
            {
                started = process.Start();
            }
            catch(Exception ex)
            {
                started = false;
                Logger.LogError("Error starting process {0}, {1} - {2}", process.StartInfo.FileName, ex, ex.Message);
            }

            if (!started)
            {
                if (OnStartFailed != null)
                    OnStartFailed(this, new EventArgs());
                return;
            }

            if (MappedExitKeyData > 0)
            {
                Logger.LogInfo("Initialising keyboard hook, Process Id: {0}", process.Id);
                keyHook = new KeyboardHook(process.Id, onMappedKey); //setup hook and attach to emu process
            }

            if (OnStarted != null)
                OnStarted(this, new EventArgs());
        }

        void waitForLaunchedProcess()
        {
            if (runningProcessIds == null)
                return;

            foreach (Process proc in System.Diagnostics.Process.GetProcessesByName(LaunchedExe))
            {
                if (runningProcessIds.Contains(proc.Id))
                    continue;
                proc.WaitForExit();
                break;
            }
        }

        void init()
        {
            if (isPC)
            {
                if (System.IO.Path.GetExtension(path).ToLower() == ".lnk")
                    initShortcut();
                else
                    initPCGame();
            }
            else
                initRom();
        }

        void initRom()
        {
            if (!File.Exists(path))
                throw new LaunchException("Unable to locate emulator exe {0}", path);

            if (shouldReplaceWildcards)
                arguments = replaceWildcards(arguments, romPath, useQuotes);
            else
                arguments = removeWildcards(arguments);
            if (string.IsNullOrEmpty(workingDirectory) || !Directory.Exists(workingDirectory))
                workingDirectory = System.IO.Path.GetDirectoryName(path);
        }

        void initPCGame()
        {
            string parsedPath, parsedArgs;
            if (path.TryGetExecutablePath(out parsedPath, out parsedArgs))
            {
                path = parsedPath;
                if (string.IsNullOrEmpty(arguments))
                    arguments = parsedArgs;
            }

            if (!File.Exists(path))
                throw new LaunchException("Unable to locate PC game {0}", path);
                
            workingDirectory = System.IO.Path.GetDirectoryName(path);
        }

        void initShortcut()
        {
            if (!File.Exists(path))
                throw new LaunchException("Unable to locate shortcut {0}", path);                
                
            try
            {
                Logger.LogDebug("Reading shortcut {0}", path);
                IWshRuntimeLibrary.IWshShell ws = new IWshRuntimeLibrary.WshShell();
                IWshRuntimeLibrary.IWshShortcut sc = (IWshRuntimeLibrary.IWshShortcut)ws.CreateShortcut(Path);
                Logger.LogDebug("\r\n\tShortcut target path: {0}\r\n\tShortcut arguments: {1}\r\n\tShortcut working directory: {2}", sc.TargetPath, sc.Arguments, sc.WorkingDirectory);
                if (!string.IsNullOrEmpty(sc.TargetPath))
                    path = sc.TargetPath;
                if (string.IsNullOrEmpty(arguments))
                    arguments = sc.Arguments;
                if (string.IsNullOrEmpty(workingDirectory))
                    workingDirectory = sc.WorkingDirectory;
            }
            catch (Exception ex)
            {
                throw new LaunchException("Error reading shortcut {0} - {1}", path, ex.Message);
            }
        }

        static string replaceWildcards(string args, string romPath, bool useQuotes)
        {
            args = args.Replace(Emulators2Settings.ROM_DIRECTORY_WILDCARD, System.IO.Path.GetDirectoryName(romPath));

            string fmt = useQuotes ? "\"{0}\"" : "{0}";
            bool foundWildcard = false;
            if (args.Contains(Emulators2Settings.GAME_WILDCARD))
            {
                foundWildcard = true;
                args = args.Replace(Emulators2Settings.GAME_WILDCARD, string.Format(fmt, romPath));
            }
            if (args.Contains(Emulators2Settings.GAME_WILDCARD_NO_EXT))
            {
                foundWildcard = true;
                string filename = System.IO.Path.GetFileNameWithoutExtension(romPath);
                args = args.Replace(Emulators2Settings.GAME_WILDCARD_NO_EXT, string.Format(fmt, filename));
            }
            if (!foundWildcard)
            {
                if (!args.EndsWith(" "))
                    args += " ";
                args += string.Format(fmt, romPath);
            }
            return args;
        }

        static string removeWildcards(string args)
        {
            return args.Replace(Emulators2Settings.GAME_WILDCARD, "").Replace(Emulators2Settings.GAME_WILDCARD_NO_EXT, "").Replace(Emulators2Settings.ROM_DIRECTORY_WILDCARD, "").Trim();
        }

        //Fired when a key press is detected by the keyboard hook
        void onMappedKey(object sender, KeyEventArgs e)
        {
            if ((int)e.KeyData == MappedExitKeyData) //key pressed is mapped key
            {
                Logger.LogDebug("Keyboard hook - Mapped key pressed, stopping emulation");
                e.Handled = true;
                e.SuppressKeyPress = true;

                int Msg;
                uint wParam;
                if (EscapeToExit)
                {
                    //set message to Esc key press
                    Msg = KeyboardHook.WM_KEYDOWN;
                    wParam = KeyboardHook.VK_ESCAPE;
                }
                else
                {
                    //Set message to window close
                    Msg = KeyboardHook.WM_CLOSE; //.WM_QUIT;
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
                    Logger.LogError("Keyboard hook - error sending close message to emulator - {0}", ex.Message);
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
