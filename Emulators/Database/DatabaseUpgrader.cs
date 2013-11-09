using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Emulators.Database
{
    class DatabaseUpgrader
    {
        ISQLiteProvider sqlClient;
        CultureInfo culture = CultureInfo.InvariantCulture;
        public DatabaseUpgrader(ISQLiteProvider sqlClient)
        {
            this.sqlClient = sqlClient;
        }

        public bool Upgrade()
        {
            if (!File.Exists(sqlClient.DatabasePath))
                return true;

            sqlClient.Init();
            SQLData result = sqlClient.Execute("SELECT value FROM Info WHERE name='version'");
            if (result.Rows.Count > 0)
            {
                double dbVersion = double.Parse(result.Rows[0].fields[0], CultureInfo.InvariantCulture);
                if (dbVersion < 1.3)
                {
                    Logger.LogError("Database is from a pre-beta version and cannot be upgraded");
                    return false;
                }

                try { upgradeToV1_7(dbVersion); }
                catch(Exception ex)
                {
                    Logger.LogError("Failed to upgrade old database to version 1.7, aborting upgrade to version 2.0 - ", ex.Message);
                    return false;
                }
            }

            Logger.LogInfo("Upgrading database to version 2");
            List<Emulator> emulators = upgradeEmulators();          
            foreach (Emulator emu in emulators)
            {
                List<Game> games = upgradeGames(emu);
                bool isPc = emu.IsPc();
                if (!isPc)
                {
                    foreach (EmulatorProfile profile in emu.EmulatorProfiles)
                        profile.Id = null;
                }
                DB.Instance.BeginTransaction();
                emu.Commit();
                foreach (Game game in games)
                {
                    if (isPc)
                    {
                        foreach (EmulatorProfile profile in game.GameProfiles)
                            profile.Id = null;
                    }
                    game.Commit();
                }
                DB.Instance.EndTransaction();
            }
            sqlClient.Dispose();
            Logger.LogInfo("Upgrade complete");
            return true;
        }

        List<Emulator> upgradeEmulators()
        {
            List<Emulator> emulators = new List<Emulator>();
            SQLData emulatorData = sqlClient.Execute("select * from Emulators");
            foreach (SQLDataRow sqlRow in emulatorData.Rows)
            {
                Emulator emu = new Emulator();
                emu.Id = int.Parse(sqlRow.fields[0]);
                emu.Title = decode(sqlRow.fields[1]);
                emu.PathToRoms = decode(sqlRow.fields[2]);
                emu.Filter = decode(sqlRow.fields[3]);
                emu.Position = int.Parse(sqlRow.fields[4]);
                emu.View = int.Parse(sqlRow.fields[5]);
                emu.Platform = decode(sqlRow.fields[6]);
                emu.Developer = decode(sqlRow.fields[7]);
                emu.Year = int.Parse(sqlRow.fields[8]);
                emu.Description = decode(sqlRow.fields[9]);
                emu.Grade = int.Parse(sqlRow.fields[10]);
                emu.VideoPreview = decode(sqlRow.fields[11]);
                int lAspect = int.Parse(sqlRow.fields[12]);
                if (lAspect != 0)
                    emu.CaseAspect = lAspect / 100.00;
                emulators.Add(emu);
            }

            foreach (Emulator emu in emulators)
            {
                upgradeProfiles(emu);
                foreach (EmulatorProfile profile in emu.EmulatorProfiles)
                {
                    if (profile.IsDefault)
                    {
                        emu.DefaultProfile = profile;
                        break;
                    }
                }
            }
            emulators.Add(createPC());
            return emulators;
        }

        List<Game> upgradeGames(Emulator parent)
        {
            List<Game> games = new List<Game>();
            SQLData gameData = sqlClient.Execute("select * from Games where parentemu=" + parent.Id);
            foreach (SQLDataRow sqlRow in gameData.Rows)
            {
                Game game = new Game();
                game.ParentEmulator = parent;
                game.Id = int.Parse(sqlRow.fields[0]);
                game.Title = decode(sqlRow.fields[3]);
                game.Grade = int.Parse(sqlRow.fields[4]);
                game.PlayCount = int.Parse(sqlRow.fields[5]);
                game.Year = int.Parse(sqlRow.fields[6]);
                game.Latestplay = DateTime.Parse(sqlRow.fields[7], culture);
                game.Description = decode(sqlRow.fields[8]);
                game.Genre = decode(sqlRow.fields[9]);
                game.Developer = decode(sqlRow.fields[10]);
                game.Favourite = bool.Parse(sqlRow.fields[12]);
                game.InfoChecked = bool.Parse(sqlRow.fields[15]);
                game.VideoPreview = decode(sqlRow.fields[17]);

                if (parent.IsPc())
                {
                    upgradeProfiles(game);
                    if (game.GameProfiles.Count < 1)
                    {
                        EmulatorProfile profile = new EmulatorProfile(true);
                        profile.Arguments = decode(sqlRow.fields[19]);
                        profile.SuspendMP = true;
                        game.GameProfiles.Add(profile);
                    }
                }

                int selectedProfileId = int.Parse(sqlRow.fields[14]);
                foreach (EmulatorProfile profile in game.EmulatorProfiles)
                {
                    if (profile.Id == selectedProfileId)
                    {
                        game.CurrentProfile = profile;
                        break;
                    }
                }

                upgradeDiscs(game);
                if (game.Discs.Count < 1)
                {
                    GameDisc disc = new GameDisc();
                    disc.Number = 1;
                    disc.Path = decode(sqlRow.fields[1]);
                    disc.LaunchFile = decode(sqlRow.fields[13]);
                    game.Discs.Add(disc);
                    game.CurrentDisc = disc;
                }
                else
                {
                    int discNum = int.Parse(sqlRow.fields[18]);
                    foreach (GameDisc disc in game.Discs)
                    {
                        if (discNum == disc.Number)
                        {
                            game.CurrentDisc = disc;
                            break;
                        }
                    }
                }
                games.Add(game);
            }
            return games;
        }

        void upgradeDiscs(Game parent)
        {
            SQLData discData = sqlClient.Execute("select * from GameDiscs where gameid=" + parent.Id);
            foreach (SQLDataRow sqlRow in discData.Rows)
            {
                GameDisc disc = new GameDisc();
                disc.Path = decode(sqlRow.fields[3]);
                disc.Number = int.Parse(sqlRow.fields[4]);
                disc.LaunchFile = decode(sqlRow.fields[5]);
                parent.Discs.Add(disc);
            }
        }

        void upgradeProfiles(Emulator parent)
        {
            SQLData profileData = sqlClient.Execute("select * from EmulatorProfiles where emulator_id=" + parent.Id);
            foreach (SQLDataRow sqlRow in profileData.Rows)
                parent.EmulatorProfiles.Add(createProfile(sqlRow));
        }

        void upgradeProfiles(Game parent)
        {
            SQLData profileData = sqlClient.Execute("select * from EmulatorProfiles where game_id=" + parent.Id);
            foreach (SQLDataRow sqlRow in profileData.Rows)
                parent.GameProfiles.Add(createProfile(sqlRow));
        }

        Emulator createPC()
        {
            Emulator emu = Emulator.GetPC();
            string title = Options.Instance.GetStringOption("pcitemtitle");
            if (title != "")
                emu.Title = title;
            emu.Developer = Options.Instance.GetStringOption("pcitemcompany");
            emu.Description = Options.Instance.GetStringOption("pcitemdescription");
            emu.Year = Options.Instance.GetIntOption("pcitemyear");
            emu.Grade = Options.Instance.GetIntOption("pcitemgrade");
            int lAspect = Options.Instance.GetIntOption("pcitemcaseaspect");
            if (lAspect != 0)
                emu.CaseAspect = lAspect / 100.00;
            emu.Position = Options.Instance.GetIntOption("pcitemposition");
            emu.VideoPreview = Options.Instance.GetStringOption("pcitemvideopreview");
            emu.View = Options.Instance.GetIntOption("viewpcgames");
            emu.PathToRoms = Options.Instance.GetStringOption("pcitemdirectory");
            emu.Filter = Options.Instance.GetStringOption("pcitemfilter");
            return emu;
        }

        EmulatorProfile createProfile(SQLDataRow sqlRow)
        {
            EmulatorProfile profile = new EmulatorProfile();
            profile.Id = int.Parse(sqlRow.fields[0]);
            profile.Title = decode(sqlRow.fields[1]);
            profile.EmulatorPath = decode(sqlRow.fields[3]);
            profile.WorkingDirectory = decode(sqlRow.fields[4]);
            profile.UseQuotes = bool.Parse(sqlRow.fields[5]);
            profile.Arguments = decode(sqlRow.fields[6]);
            profile.SuspendMP = bool.Parse(sqlRow.fields[7]);
            profile.MountImages = bool.Parse(sqlRow.fields[8]);
            profile.EscapeToExit = bool.Parse(sqlRow.fields[9]);
            profile.CheckController = bool.Parse(sqlRow.fields[10]);
            profile.IsDefault = bool.Parse(sqlRow.fields[11]);
            profile.PreCommand = decode(sqlRow.fields[12]);
            profile.PreCommandWaitForExit = bool.Parse(sqlRow.fields[13]);
            profile.PreCommandShowWindow = bool.Parse(sqlRow.fields[14]);
            profile.PostCommand = decode(sqlRow.fields[15]);
            profile.PostCommandWaitForExit = bool.Parse(sqlRow.fields[16]);
            profile.PostCommandShowWindow = bool.Parse(sqlRow.fields[17]);
            profile.LaunchedExe = decode(sqlRow.fields[19]);
            profile.StopEmulationOnKey = boolFromInt(int.Parse(sqlRow.fields[20]));
            profile.DelayResume = bool.Parse(sqlRow.fields[21]);
            profile.ResumeDelay = int.Parse(sqlRow.fields[22]);
            profile.GoodmergeTags = decode(sqlRow.fields[23]);
            profile.EnableGoodmerge = bool.Parse(sqlRow.fields[24]);
            return profile;
        }

        private static bool? boolFromInt(int i)
        {
            if (i < 0)
                return null;
            return i > 0;
        }
        
        static string decode(string input)
        {
            if (input == null)
            {
                return null;
            }
            else
            {
                String decoded = input;
                decoded = decoded.Replace("&#39;", "'");
                decoded = decoded.Replace("&#34;", "\"");
                decoded = decoded.Replace("&#123;", "{");
                decoded = decoded.Replace("&#125;", "}");
                return decoded;
            }
        }

        void upgradeToV1_7(double dbVersion)
        {
            sqlClient.Execute("BEGIN");
            if (dbVersion < 1.4)
            {
                sqlClient.Execute("ALTER TABLE EmulatorProfiles ADD COLUMN stopemulation int");
                sqlClient.Execute("UPDATE EmulatorProfiles SET stopemulation=-1");
            }

            if (dbVersion < 1.5)
            {
                sqlClient.Execute("ALTER TABLE EmulatorProfiles ADD COLUMN delayresume char(5)");
                sqlClient.Execute("ALTER TABLE EmulatorProfiles ADD COLUMN resumedelay int");
                sqlClient.Execute("UPDATE EmulatorProfiles SET delayresume='False', resumedelay=500");
            }

            if (dbVersion < 1.6)
            {
                string profileTableString =
                "uid int, title varchar(200), emulator_id int, emulator_path varchar(200), working_path varchar(200), usequotes char(5), args varchar(200), suspend_mp char(5), mountimages char(5), escapetoexit char(5), checkcontroller char(5), defaultprofile char(5), precommand varchar(200), prewaitforexit char(5), preshowwindow char(5), postcommand varchar(200), postwaitforexit char(5), postshowwindow char(5), game_id int, launchedexe varchar(200), stopemulation int, delayresume char(5), resumedelay int, goodmergepref varchar(200), PRIMARY KEY(uid)";

                sqlClient.Execute("ALTER TABLE EmulatorProfiles RENAME TO emuprofiletemp");
                sqlClient.Execute(string.Format("CREATE TABLE EmulatorProfiles({0})", profileTableString));
                sqlClient.Execute("INSERT INTO EmulatorProfiles SELECT uid, title, emulator_id, emulator_path, working_path, usequotes, args, suspend_mp, mountimages, escapetoexit, checkcontroller, defaultprofile, precommand, prewaitforexit, preshowwindow, postcommand, postwaitforexit, postshowwindow, game_id, launchedexe, stopemulation, delayresume, resumedelay, '' FROM emuprofiletemp");
                
                SQLData currentPrefixes = sqlClient.Execute("SELECT uid, goodmerge_pref1, goodmerge_pref2, goodmerge_pref3 FROM emuprofiletemp");
                foreach(SQLDataRow row in currentPrefixes.Rows)
                {
                    string prefix = row.fields[1];
                    if (!string.IsNullOrEmpty(row.fields[2]))
                    {
                        if (!string.IsNullOrEmpty(prefix))
                            prefix += ";";
                        prefix += row.fields[2];
                    }
                    if (!string.IsNullOrEmpty(row.fields[3]))
                    {
                        if (!string.IsNullOrEmpty(prefix))
                            prefix += ";";
                        prefix += row.fields[3];
                    }
                    if (!string.IsNullOrEmpty(prefix))
                        sqlClient.Execute(string.Format("UPDATE EmulatorProfiles SET goodmergepref='{0}' WHERE uid={1}", prefix, row.fields[0]));
                }
                sqlClient.Execute("DROP TABLE emuprofiletemp");
            }

            if (dbVersion < 1.7)
            {
                string emuTableString =
                "uid int, title varchar(50), rom_path varchar(200), filter varchar(100), position int, view int, platformtitle varchar(50), Company varchar(200), Yearmade int, Description varchar(2000), Grade int, videopreview varchar(200), caseaspect int, isarcade char(5), PRIMARY KEY(uid)";

                sqlClient.Execute("ALTER TABLE Emulators RENAME TO emustemp");
                sqlClient.Execute(string.Format("CREATE TABLE Emulators({0})", emuTableString));
                sqlClient.Execute("INSERT INTO Emulators SELECT uid, title, rom_path, filter, position, view, platformtitle, Company, Yearmade, Description, Grade, videopreview, caseaspect, 'False' FROM emustemp");
                sqlClient.Execute("ALTER TABLE EmulatorProfiles ADD COLUMN enablegoodmerge char(5)");
                sqlClient.Execute("UPDATE EmulatorProfiles SET enablegoodmerge='False'");

                SQLData enableGoodmerge = sqlClient.Execute("SELECT uid, enable_goodmerge FROM emustemp");
                foreach(SQLDataRow row in enableGoodmerge.Rows)
                {
                    if (row.fields[1] == "True")
                        sqlClient.Execute(string.Format("UPDATE EmulatorProfiles SET enablegoodmerge='True' WHERE emulator_id={0}", row.fields[0]));
                }
                sqlClient.Execute("DROP TABLE emustemp");
            }
            sqlClient.Execute("COMMIT");
        }
    }
}
