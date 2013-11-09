using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Emulators.Import
{
    public class ScraperProvider : IDoWork
    {
        #region Load Scrapers
        static object allScraperSync = new object();
        static List<Scraper> allScrapers;
        static void loadScrapers()
        {
            lock (allScraperSync)
            {
                if (allScrapers == null)
                {
                    allScrapers = new List<Scraper>();
                    OfflineMameScraper mameScraper = new OfflineMameScraper();
                    if (mameScraper.IsReady)
                        allScrapers.Add(new OfflineMameScraper());

                    foreach (string resource in System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceNames())
                    {
                        if (!resource.StartsWith("Emulators.Import.Scripts.") || !resource.EndsWith(".xml"))
                            continue;

                        ScriptScraper script = new ScriptScraper(resource);
                        if (script.IsReady)
                            allScrapers.Add(script);
                        else
                            Logger.LogDebug("Failed to parse scraper script {0}", resource);
                    }
                }
            }
        }

        public static List<Scraper> GetScrapers(bool allowIgnored = false)
        {
            loadScrapers();

            List<string> scraperPriorities = new List<string>(Options.Instance.GetStringOption("scraperpriorities").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));            
            List<string> ignoredScraperIds = new List<string>();
            if (!allowIgnored)
            {
                ignoredScraperIds.AddRange(Options.Instance.GetStringOption("ignoredscrapers").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
            }

            List<Scraper> scrapers = new List<Scraper>();
            foreach (Scraper scraper in allScrapers)
            {
                if (allowIgnored || !ignoredScraperIds.Contains(scraper.IdString))
                {
                    if (!scraperPriorities.Contains(scraper.IdString))
                        scraperPriorities.Add(scraper.IdString);
                    scrapers.Add(scraper);
                }
            }

            scrapers.Sort((Scraper s, Scraper t) =>
            {
                return scraperPriorities.IndexOf(s.IdString).CompareTo(scraperPriorities.IndexOf(t.IdString));
            });
            return scrapers;
        }
        #endregion

        object scraperSync = new object();
        List<Scraper> scrapers = null;
        Scraper coversScraper = null;
        Scraper screensScraper = null;
        Scraper fanartScraper = null;

        bool importTop = false;
        bool importExact = false;
        bool thoroughThumbSearch = false;

        public ScraperProvider()
        {
            Update();
        }

        public void Update()
        {
            importTop = Options.Instance.GetBoolOption("importtop");
            importExact = Options.Instance.GetBoolOption("importexact");
            thoroughThumbSearch = Options.Instance.GetBoolOption("thoroughthumbsearch");

            string coversScraperId = Options.Instance.GetStringOption("coversscraperid");
            string screensScraperId = Options.Instance.GetStringOption("screensscraperid");
            string fanartScraperId = Options.Instance.GetStringOption("fanartscraperid");
            lock (scraperSync)
            {
                scrapers = GetScrapers();
                foreach (Scraper scraper in scrapers)
                {
                    if (scraper.IdString == coversScraperId)
                        coversScraper = scraper;
                    if (scraper.IdString == screensScraperId)
                        screensScraper = scraper;
                    if (scraper.IdString == fanartScraperId)
                        fanartScraper = scraper;
                }
            }
        }

        bool doWork()
        {
            if (DoWork != null)
                return DoWork();
            return true;
        }

        #region Search

        public List<ScraperResult> GetMatches(RomMatch romMatch, out ScraperResult bestResult, out bool approved)
        {
            bestResult = null;
            approved = false;
            List<Scraper> lScrapers;

            lock (scraperSync)
                lScrapers = new List<Scraper>(scrapers);

            //get parent title to try and match platform
            string searchPlatform = romMatch.Game.ParentEmulator.Platform;
            if (searchPlatform == "Unspecified")
                searchPlatform = "";

            string searchTerm = RemoveSpecialChars(romMatch.SearchTitle);
            string filename = romMatch.IsDefaultSearchTerm ? romMatch.Filename : null;
            ScraperSearchParams searchParams = new ScraperSearchParams()
            {
                Term = searchTerm,
                Platform = searchPlatform,
                Filename = filename
            };

            List<ScraperResult> results = new List<ScraperResult>();
            foreach (Scraper scraper in lScrapers)
            {
                if (!doWork())
                    return null;

                results.AddRange(scraper.GetMatches(searchParams));
            }

            searchPlatform = searchPlatform.ToLower();
            results = sortResults(results, searchPlatform, out approved);
            if (results.Count > 0)
                bestResult = results[0];

            return results;
        }

        List<ScraperResult> sortResults(List<ScraperResult> results, string platform, out bool approved)
        {
            approved = false;
            if (results.Count < 1)
                return new List<ScraperResult>();

            List<string> resultIds = new List<string>();
            List<ScraperResult> matches = new List<ScraperResult>();
            List<ScraperResult> nonPlatformMatches = new List<ScraperResult>();
            bool isPlatform;
            foreach (ScraperResult result in results)
            {
                if (!resultIds.Contains(result.SiteId))
                {
                    resultIds.Add(result.SiteId);
                    if (string.IsNullOrEmpty(platform) || result.System.ToLower() == platform)
                        matches.Add(result);
                    else
                        nonPlatformMatches.Add(result);
                }
            }

            matches.Sort();
            nonPlatformMatches.Sort();

            isPlatform = matches.Count > 0;
            matches.AddRange(nonPlatformMatches);
            if (matches.Count > 0)
            {
                int distance = matches[0].SearchDistance;
                approved = importTop || (distance == 0 && isPlatform) || (!importExact && distance < 3 && isPlatform);
            }
            return matches;
        }

        #endregion

        #region Download

        public ScraperGame DownloadInfo(ScraperResult result)
        {
            Scraper defaultScraper = result.DataProvider;
            ScraperGame scraperGame = defaultScraper.GetDetails(result);
            if (scraperGame == null || !doWork())
                return null;

            Scraper coversScraper, screensScraper, fanartScraper;
            lock (scraperSync)
            {
                coversScraper = this.coversScraper;
                screensScraper = this.screensScraper;
                fanartScraper = this.fanartScraper;
            }

            ScraperResultsCache resultsCache = new ScraperResultsCache(result.Title, result.SearchParams);
            resultsCache.Add(defaultScraper, result);

            string image1, image2;
            List<Scraper> searchScrapers = getSearchScrapers(coversScraper, defaultScraper);
            if (!searchForImages(searchScrapers, resultsCache, ThumbSearchType.Covers, out image1, out image2))
                return null; //doWork is false
            scraperGame.BoxFrontUrl = image1;
            scraperGame.BoxBackUrl = image2;

            searchScrapers = getSearchScrapers(screensScraper, defaultScraper);
            if (!searchForImages(searchScrapers, resultsCache, ThumbSearchType.Screens, out image1, out image2))
                return null;
            scraperGame.TitleScreenUrl = image1;
            scraperGame.InGameUrl = image2;

            searchScrapers = getSearchScrapers(fanartScraper, defaultScraper);
            searchForImages(searchScrapers, resultsCache, ThumbSearchType.Fanart, out image1, out image2);
            scraperGame.FanartUrl = image1;

            return scraperGame;
        }

        bool searchForImages(List<Scraper> lScrapers, ScraperResultsCache resultsCache, ThumbSearchType searchType, out string image1, out string image2)
        {
            image1 = null; 
            image2 = null;
            foreach (Scraper scraper in lScrapers)
            {
                string l1, l2;
                ScraperResult result = resultsCache.GetResult(scraper);
                if (!doWork())
                    return false;
                if (result != null)
                {
                    if (searchType == ThumbSearchType.Fanart)
                    {
                        scraper.GetFanartUrls(result, out image1);
                    }
                    else
                    {
                        if (searchType == ThumbSearchType.Covers)
                            scraper.GetCoverUrls(result, out l1, out l2);
                        else
                            scraper.GetScreenUrls(result, out l1, out l2);
                        if (image1 == null)
                            image1 = l1;
                        if (image2 == null)
                            image2 = l2;
                    }

                    if (image1 != null && (image2 != null || searchType == ThumbSearchType.Fanart))
                        break;
                    if (!doWork())
                        return false;
                }
            }
            return true;
        }

        List<Scraper> getSearchScrapers(Scraper preferredScraper, Scraper defaultScraper)
        {
            List<Scraper> lScrapers;
            if (thoroughThumbSearch)
            {
                lock (scraperSync)
                    lScrapers = new List<Scraper>(scrapers);
                int offset = 0;
                if (preferredScraper != null)
                {
                    lScrapers.Remove(preferredScraper);
                    lScrapers.Insert(0, preferredScraper);
                    offset = 1;
                }
                if (defaultScraper != preferredScraper)
                {
                    lScrapers.Remove(defaultScraper);
                    lScrapers.Insert(offset, defaultScraper);
                }
            }
            else
            {
                lScrapers = new List<Scraper>();
                if (preferredScraper != null)
                    lScrapers.Add(preferredScraper);
                if (defaultScraper != preferredScraper)
                    lScrapers.Add(defaultScraper);
            }
            return lScrapers;
        }

        #endregion

        public static string RemoveSpecialChars(string s)
        {
            s = s.ToLower();
            s = Regex.Replace(s, @"[(\[].*?[)\]]", "");
            s = Regex.Replace(s, @",\s*the\b", "");
            s = Regex.Replace(s, @"^the\b", "");
            s = Regex.Replace(s, "[/,'°]", "");
            s = Regex.Replace(s, "[_:-]", " ");
            s = Regex.Replace(s, @"\band\b", "&");
            s = Regex.Replace(s, @"\s\s+", " ");
            s = s.Replace("é", "e");
            return s.Trim();
        }

        enum ThumbSearchType
        {
            Covers,
            Screens,
            Fanart
        }

        #region IDoWork Members

        public event DoWorkDelegate DoWork;

        #endregion
    }
}
