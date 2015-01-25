using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Emulators.Launcher
{
    public class GameLauncher
    {        
        Game game;
        GameProcess gameProcess;
        string romPath;

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

        public event EventHandler<ExtractionEventArgs> ExtractionProgress;
        protected virtual void OnExtractionProgress(ExtractionEventArgs e)
        {
            if (ExtractionProgress != null)
                ExtractionProgress(this, e);
        }

        public event EventHandler ExtractionFailed;
        protected virtual void OnExtractionFailed()
        {
            if (ExtractionFailed != null)
                ExtractionFailed(this, EventArgs.Empty);
        }

        public Game Game { get { return game; } }
        public string RomPath { get { return romPath; } }

        public GameLauncher(Game game)
        {
            this.game = game;
        }

        public void Launch()
        {
            romPath = game.CurrentDisc.Path;
            if (game.IsGoodmerge)
            {
                romPath = extractGame(romPath);
                if (romPath == null)
                {
                    OnExtractionFailed();
                    return;
                }
            }
            startGame();
        }

        string extractGame(string path)
        {
            using (SharpCompressExtractor extractor = new SharpCompressExtractor(path))
            {
                extractor.ExtractionProgress += extractor_ExtractionProgress;
                extractor.Init();
                if (extractor.Files != null && extractor.Files.Count > 0)
                {
                    int index = GoodmergeHandler.GetFileIndex(game.CurrentDisc.LaunchFile, extractor.Files, game.CurrentProfile.GetGoodmergeTags());
                    return extractor.Extract(extractor.Files[index], GoodmergeHandler.GetExtractionDirectory(path));
                }
            }
            return path;
        }

        void startGame()
        {
            gameProcess = new GameProcess(romPath, game.CurrentProfile, game.ParentEmulator.IsPc());
            gameProcess.Starting += gameProcess_Starting;
            gameProcess.Started += gameProcess_Started;
            gameProcess.StartFailed += gameProcess_StartFailed;
            gameProcess.Exited += gameProcess_Exited;
            gameProcess.Start();
        }

        void extractor_ExtractionProgress(object sender, ExtractionEventArgs e)
        {
            OnExtractionProgress(e);
        }

        void gameProcess_Starting(object sender, EventArgs e)
        {
            OnStarting();
        }

        void gameProcess_Started(object sender, EventArgs e)
        {
            OnStarted();
        }

        void gameProcess_StartFailed(object sender, EventArgs e)
        {
            OnStartFailed();
            gameProcess.Dispose();
        }

        void gameProcess_Exited(object sender, EventArgs e)
        {
            OnExited();
            gameProcess.Dispose();
        }
    }
}
