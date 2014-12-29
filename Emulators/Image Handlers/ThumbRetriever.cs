using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading;
using Emulators.Import;

namespace Emulators
{
    public delegate void ThumbRetrieverStatusChangedHandler(ThumbRetrieverStatus status);
    public delegate void SearchCompletedHandler(RomMatch romMatch);
    public delegate void ThumbDownloadedHandler(Bitmap image, bool isCovers);
    public delegate void ThumbDownloadCompleteHandler();

    public delegate void PlatformsFoundHandler(Dictionary<string, string> platforms, string selectedKey);

    public class ThumbRetriever
    {
        object syncRoot = new object();
        volatile bool doWork = true;
        ScraperProvider scraperProvider = null;

        Thread controllerThread = null;
        Thread searchThread = null;
        Thread thumbThread1 = null;
        Thread thumbThread2 = null;

        public event ThumbRetrieverStatusChangedHandler OnStatusChanged;
        public event ThumbDownloadedHandler OnThumbDownloaded;
        public event SearchCompletedHandler OnSearchCompleted;
        public event ThumbDownloadCompleteHandler OnDownloadComplete;
        public event PlatformsFoundHandler OnPlatformsFound;

        public ThumbRetriever()
        {
            scraperProvider = new ScraperProvider();
            scraperProvider.DoWork += new DoWorkDelegate(() => doWork);
        }

        public void Search(string title, Game game)
        {
            startSearch(false, title, game);
        }

        public void GetPlatforms(string platform)
        {
            startSearch(true, platform, null);
        }

        void startSearch(bool isPlatform, string title, Game game)
        {
            if (controllerThread != null && controllerThread.IsAlive)
                controllerThread.Join();

            controllerThread = new Thread(delegate()
                {
                    lock (syncRoot)
                    {
                        stop();
                        if (OnStatusChanged != null)
                            OnStatusChanged(ThumbRetrieverStatus.Searching);
                        if (isPlatform)
                            searchPlatforms(title);
                        else
                            searchGame(title, game);
                    }
                }) { Name = "ThumbRetrieverController", IsBackground = true };
            controllerThread.Start();
        }

        void searchGame(string title, Game game)
        {
            RomMatch romMatch = new RomMatch(game);
            romMatch.SearchTitle = title; //don't alter game's title
            scraperProvider.Update();
            searchThread = new Thread(new ThreadStart(delegate()
                {
                    try
                    {
                        ScraperResult result; bool approved; 
                        romMatch.PossibleGameDetails = scraperProvider.GetMatches(romMatch, out result, out approved);
                        romMatch.GameDetails = result;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex);
                    }
                    finally
                    {
                        if (OnSearchCompleted != null)
                            OnSearchCompleted(romMatch); 
                        if (OnStatusChanged != null)
                            OnStatusChanged(ThumbRetrieverStatus.Ready);
                    }
                }
                )) { Name = "ThumbSearchThread" };
            searchThread.Start();
        }

        void searchPlatforms(string platform)
        {
            searchThread = new Thread(new ThreadStart(delegate()
            {
                Dictionary<string, string> platforms = null;
                string selectedKey = null;
                try
                {
                    platforms = new EmulatorScraper().GetEmulators(platform, out selectedKey);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex);
                }
                finally
                {
                    if (OnPlatformsFound != null)
                        OnPlatformsFound(platforms, selectedKey);
                    if (OnStatusChanged != null)
                        OnStatusChanged(ThumbRetrieverStatus.Ready);
                }
            }
                )) { Name = "ThumbSearchThread" };
            searchThread.Start();
        }
                
        public void RetrieveThumbs(RomMatch romMatch)
        {
            startThumbRetrieval(romMatch, null);
        }

        public void RetrieveThumbs(string platformId)
        {
            startThumbRetrieval(null, platformId);
        }

        void startThumbRetrieval(RomMatch romMatch, string platformId)
        {
            if (controllerThread != null && controllerThread.IsAlive)
                controllerThread.Join();

            controllerThread = new Thread(delegate()
            {
                lock (syncRoot)
                {
                    stop();
                    if (OnStatusChanged != null)
                        OnStatusChanged(ThumbRetrieverStatus.Downloading);
                    retrieveThumbs(romMatch, platformId);
                }
            }) { Name = "ThumbRetrieverController", IsBackground = true };
            controllerThread.Start();
        }

        //This is written a bit weird, we initialise both threads before starting
        //then first thread retrieves list of thumb url's, then starts second thread dloading screens,
        //then starts dloading covers itself 
        void retrieveThumbs(RomMatch romMatch, string platformId)
        {
            if (romMatch != null)
            {
                if (romMatch.GameDetails == null)
                    return;
            }
            else if (platformId == null)
                return;

            IEnumerable<string> covers = null;
            IEnumerable<string> screens = null;
            IEnumerable<string> fanarts = null;

            //initialise first thread
            thumbThread1 = new Thread(new ThreadStart(delegate()
            {
                try
                {
                    //get thumb url's
                    if (romMatch != null)
                    {
                        Scraper scraper = romMatch.GameDetails.DataProvider;
                        covers = scraper.GetCoverUrls(romMatch.GameDetails, false);
                        screens = scraper.GetScreenUrls(romMatch.GameDetails, false);
                        fanarts = scraper.GetFanartUrls(romMatch.GameDetails, false);
                    }
                    else
                        new EmulatorScraper().GetThumbs(platformId, out covers, out screens);

                    if (!doWork)
                        return;

                    //start second thread retrieving screens
                    thumbThread2.Start();
                    //then retrieve covers
                    downloadThumbs(covers, true);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex);
                }
                finally
                {
                    if (thumbThread2 != null && thumbThread2.IsAlive)
                        thumbThread2.Join();
                    if (OnDownloadComplete != null)
                        OnDownloadComplete();
                    if (OnStatusChanged != null)
                        OnStatusChanged(ThumbRetrieverStatus.Ready);
                }
            }
            )) { Name = "ThumbDownloadThread1" };

            //initialise second thread
            thumbThread2 = new Thread(new ThreadStart(delegate()
            {
                try
                {
                    downloadThumbs(screens, false);
                    downloadThumbs(fanarts, false);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex);
                }
            }
            )) { Name = "ThumbDownloadThread2" };

            //start dload
            thumbThread1.Start();
        }

        public void Stop()
        {
            if (controllerThread != null && controllerThread.IsAlive)
                controllerThread.Join();

            controllerThread = new Thread(delegate()
            {
                lock (syncRoot)
                {
                    stop();
                }
            }) { Name = "ThumbRetrieverController", IsBackground = true };
            controllerThread.Start();
        }

        void stop()
        {
            doWork = false; 
            if (OnStatusChanged != null)
                OnStatusChanged(ThumbRetrieverStatus.Stopping);
            if (searchThread != null && searchThread.IsAlive)
            {
                searchThread.Join();
                searchThread = null;
            }
            if (thumbThread1 != null && thumbThread1.IsAlive)
            {
                thumbThread1.Join();
                thumbThread1 = null;
            }
            if (thumbThread2 != null && thumbThread2.IsAlive)
            {
                thumbThread2.Join();
                thumbThread2 = null;
            }
            doWork = true;
            if (OnStatusChanged != null)
                OnStatusChanged(ThumbRetrieverStatus.Stopped);
        }

        void downloadThumbs(IEnumerable<string> thumbs, bool isCovers)
        {
            if (thumbs == null)
                return;

            foreach (string thumb in thumbs)
            {
                if (!doWork)
                    return;
                Bitmap image = getImage(thumb);
                if (image != null && OnThumbDownloaded != null)
                {
                    OnThumbDownloaded(image, isCovers);
                }
            }
        }

        Bitmap getImage(string url)
        {
            Bitmap img = null;
            BitmapDownloadResult result = ImageHandler.BeginBitmapFromWeb(url);
            if (result != null)
            {
                bool cancel = false;
                while (!result.IsCompleted)
                {
                    if (!doWork)
                    {
                        cancel = true;
                        break;
                    }
                    System.Threading.Thread.Sleep(100);
                }
                if (cancel)
                {
                    result.Cancel();
                    return null;
                }
                img = result.Bitmap;
            }
            return img;
        }
    }

    public enum ThumbRetrieverStatus
    {
        Ready,
        Stopping,
        Stopped,
        Searching,
        Downloading
    }
}
