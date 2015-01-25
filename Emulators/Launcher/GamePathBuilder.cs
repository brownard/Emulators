using IWshRuntimeLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Emulators.Launcher
{
    public class GamePathBuilder
    {
        #region Wildcards
        public const string ROM_DIRECTORY_WILDCARD = "%ROMDIRECTORY%";
        public const string GAME_WILDCARD = "%ROM%";
        public const string GAME_WILDCARD_NO_EXT = "%ROM_WITHOUT_EXT%";
        #endregion

        public static GamePath CreateRomPath(string emulatorPath, string arguments, string romPath, bool replaceWildcards, bool useQuotes)
        {
            if (replaceWildcards)
            {
                arguments = arguments.Replace(ROM_DIRECTORY_WILDCARD, Path.GetDirectoryName(romPath));
                string format = useQuotes ? "\"{0}\"" : "{0}";

                if (!arguments.Contains(GAME_WILDCARD) && !arguments.Contains(GAME_WILDCARD_NO_EXT))
                {
                    if (!arguments.EndsWith(" "))
                        arguments += " ";
                    arguments += string.Format(format, romPath);
                }
                else
                {
                    arguments = arguments.Replace(GAME_WILDCARD, string.Format(format, romPath));
                    arguments = arguments.Replace(GAME_WILDCARD_NO_EXT, string.Format(format, Path.GetFileNameWithoutExtension(romPath)));
                }
            }
            else
            {
                arguments = cleanArguments(arguments);
            }

            return new GamePath()
            {
                Path = emulatorPath,
                Arguments = arguments,
                WorkingDirectory = Path.GetDirectoryName(emulatorPath)
            };
        }

        public static GamePath CreatePCPath(string path, string arguments)
        {
            if (isShortcut(path))
                return CreateShortcutPath(path, arguments);

            string parsedPath, parsedArgs;
            if (path.TryGetExecutablePath(out parsedPath, out parsedArgs))
            {
                path = parsedPath;
                if (string.IsNullOrEmpty(arguments))
                    arguments = parsedArgs;
            }

            return new GamePath()
            {
                Path = path,
                Arguments = arguments,
                WorkingDirectory = Path.GetDirectoryName(path)
            };
        }

        public static GamePath CreateShortcutPath(string path, string arguments)
        {
            string workingDirectory = null;
            try
            {
                IWshShell ws = new WshShell();
                IWshShortcut sc = (IWshShortcut)ws.CreateShortcut(path);
                Logger.LogDebug("\r\n\tShortcut target path: {0}\r\n\tShortcut arguments: {1}\r\n\tShortcut working directory: {2}", sc.TargetPath, sc.Arguments, sc.WorkingDirectory);
                if (!string.IsNullOrEmpty(sc.TargetPath))
                    path = sc.TargetPath;
                if (string.IsNullOrEmpty(arguments))
                    arguments = sc.Arguments;
                workingDirectory = sc.WorkingDirectory;
            }
            catch (Exception ex)
            {
                Logger.LogError("GamePathBuilder: Error reading shortcut {0} - {1}", path, ex.Message);
            }

            return new GamePath()
            {
                Path = path,
                Arguments = arguments,
                WorkingDirectory = workingDirectory
            };
        }

        static string cleanArguments(string arguments)
        {
            return arguments.Replace(GAME_WILDCARD, "").Replace(GAME_WILDCARD_NO_EXT, "").Replace(ROM_DIRECTORY_WILDCARD, "").Trim();
        }

        static bool isShortcut(string path)
        {
            return Path.GetExtension(path).ToLower() == ".lnk";
        }
    }

    public class GamePath
    {
        public string Path { get; set; }
        public string Arguments { get; set; }
        public string WorkingDirectory { get; set; }
    }
}
