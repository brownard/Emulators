using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Emulators.Database
{
    class DBRefresh
    {
        public bool RefreshDatabase()
        {
            Logger.LogDebug("Refreshing database");
            HashSet<string> filesToIgnore = getFilesToIgnore();
            List<Game> newGames = new List<Game>();
            List<Emulator> emus = Emulator.GetAll();
            foreach (Emulator emu in emus)
                getNewGames(emu, newGames, filesToIgnore);

            Logger.LogDebug("Found {0} new game(s)", newGames.Count);
            if (newGames.Count != 0)
            {
                EmulatorsCore.Database.BeginTransaction();
                foreach (Game game in newGames)
                    game.Commit();
                EmulatorsCore.Database.EndTransaction();
                return true;
            }
            return false;
        }

        public void DeleteMissingGames()
        {
            HashSet<string> drivesToIgnore = new HashSet<string>();
            List<Game> games = Game.GetAll();
            EmulatorsCore.Database.BeginTransaction();
            foreach (Game game in games)
                checkDiscs(game, drivesToIgnore);
            EmulatorsCore.Database.EndTransaction();
        }

        void getNewGames(Emulator emu, List<Game> newGames, HashSet<string> filesToIgnore)
        {
            string romDir = emu.PathToRoms;
            if (string.IsNullOrEmpty(romDir) || !System.IO.Directory.Exists(romDir))
            {
                Logger.LogWarn("Could not locate {0} rom directory '{1}'", emu.Title, romDir);
                return;
            }

            string[] filters = emu.Filter.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string filter in filters)
            {
                string[] gamePaths = getFiles(romDir, filter);
                if (gamePaths == null)
                    continue;

                foreach (string path in gamePaths)
                {
                    if (!filesToIgnore.Contains(path))
                    {
                        filesToIgnore.Add(path);
                        newGames.Add(new Game(emu, path));
                    }
                }
            }
        }

        HashSet<string> getFilesToIgnore()
        {
            List<string> dbPaths = EmulatorsCore.Database.GetAll<GameDisc>().Select(g => g.Path).ToList();
            List<string> ignoredFiles = EmulatorsCore.Options.IgnoredFiles();
            HashSet<string> files = new HashSet<string>();
            foreach (string dbPath in dbPaths)
                files.Add(dbPath);
            foreach (string ignoredFile in ignoredFiles)
                files.Add(ignoredFile);
            return files;
        }

        string[] getFiles(string directory, string filter)
        {
            try
            {
                return Directory.GetFiles(directory, filter, SearchOption.AllDirectories);
            }
            catch
            {
                Logger.LogError("Error locating files in '{0}' using filter '{1}'", directory, filter);
                return null;
            }
        }

        void checkDiscs(Game game, HashSet<string> drivesToIgnore)
        {
            List<GameDisc> missingDiscs = new List<GameDisc>();
            foreach (GameDisc disc in game.Discs)
                checkDiscExists(disc, missingDiscs, drivesToIgnore);

            if (missingDiscs.Count == game.Discs.Count)
            {
                Logger.LogDebug("Removing {0} from the database, file not found", game.Title);
                game.Delete();
            }
            else if (missingDiscs.Count > 0)
            {
                updateDiscs(game, missingDiscs);
            }
        }

        void checkDiscExists(GameDisc disc, List<GameDisc> missingDiscs, HashSet<string> drivesToIgnore)
        {
            string path = disc.Path;
            string drive = Path.GetPathRoot(path);
            if (!drivesToIgnore.Contains(drive))
            {
                //if path root is missing assume file is on disconnected
                //removable/network drive and don't delete
                if (!Directory.Exists(drive))
                    drivesToIgnore.Add(drive);
                else if (!File.Exists(path))
                    missingDiscs.Add(disc);
            }
        }

        void updateDiscs(Game game, List<GameDisc> missingDiscs)
        {
            foreach (GameDisc disc in missingDiscs)
            {
                Logger.LogDebug("Removing disc {0}, file not found", disc.Path);
                game.Discs.Remove(disc);
                disc.Delete();
            }
            game.Discs.Commit();
        }
    }
}
