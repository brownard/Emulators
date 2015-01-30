using Emulators.Import;
using Emulators.Scrapers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Emulators.Image_Handlers
{
    public class GameImageDownloader : ImageDownloadHandler
    {
        ScraperProvider scraperProvider;
        AutoResetEvent currentTask = new AutoResetEvent(true);
        bool doWork = true;

        public event EventHandler<GameSearchCompletedEventArgs> SearchCompleted;
        protected virtual void OnSearchCompleted(GameSearchCompletedEventArgs e)
        {
            var searchCompleted = SearchCompleted;
            if (searchCompleted != null)
                searchCompleted(this, e);
        }

        public GameImageDownloader(ScraperProvider scraperProvider)
        {
            this.scraperProvider = scraperProvider;
            scraperProvider.DoWork += () => doWork;
        }

        public void Search(string term, Game game)
        {
            invokeAsync(() =>
                {
                    RomMatch romMatch = new RomMatch(game) { SearchTitle = term };
                    scraperProvider.Update();
                    ScraperResult result;
                    bool approved;
                    List<ScraperResult> results = scraperProvider.GetMatches(romMatch, out result, out approved);
                    OnSearchCompleted(new GameSearchCompletedEventArgs(results, result));
                });
        }

        public void DownloadImages(ScraperResult result)
        {
            invokeAsync(() =>
            {
                Scraper scraper = result.DataProvider;
                List<string> coverUrls = scraper.GetCoverUrls(result);
                List<string> screenUrls = scraper.GetScreenUrls(result);
                screenUrls.AddRange(scraper.GetFanartUrls(result));
                BeginDownload(coverUrls, screenUrls);
            }); 
        }

        public override void CancelDownload()
        {
            invokeAsync(() =>
            {
                doWork = false;
                base.CancelDownload();
                currentTask.Close();
            });
        }

        void invokeAsync(Action action)
        {
            new Thread(() =>
            {
                try
                {
                    currentTask.WaitOne();
                    action();
                    currentTask.Set();
                }
                catch { }
            }) { IsBackground = true }
            .Start();
        }
    }

    public class GameSearchCompletedEventArgs : EventArgs
    {
        public GameSearchCompletedEventArgs(List<ScraperResult> results, ScraperResult result)
        {
            Results = results;
            Result = result;
        }

        public List<ScraperResult> Results { get; private set; }
        public ScraperResult Result { get; private set; }
    }
}
