using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using System.Text.RegularExpressions;
using System.Drawing;
using Cornerstone.ScraperEngine;

namespace Emulators.Import
{
    /// <summary>
    /// A wrapper for a specified script, provides
    /// methods to search and download info.
    /// </summary>
    public class ScriptScraper : Scraper
    {
        #region Variables
        
        const string FRONT_IMAGE_TAG = "front";
        const string BACK_IMAGE_TAG = "back";
        const string TITLE_IMAGE_TAG = "title";

        ScriptableScraper scraper;
        string name;
        string idString = "-1";
        bool retrievesDetails;
        bool retrievesCovers;
        bool retrievesScreens;
        bool retrievesFanart;

        #endregion

        #region Ctor

        public static ScriptScraper TryCreate(string path, bool fromAssembly)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            string scriptTxt = null;
            try
            {
                Stream scriptStream;
                if(fromAssembly)
                    scriptStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
                else
                    scriptStream = File.OpenRead(path);

                using (StreamReader sr = new StreamReader(scriptStream))
                    scriptTxt = sr.ReadToEnd();
            }
            catch (Exception ex)
            {
                Logger.LogError("ScriptScraper: Error loading script from '{0}' - {1}", path, ex.Message);
                return null;
            }

            if (string.IsNullOrEmpty(scriptTxt))
                return null;
            ScriptableScraper scraper = new ScriptableScraper(scriptTxt, false);
            if (!scraper.LoadSuccessful)
            {
                Logger.LogError("ScriptScraper: Error loading script from '{0}'", path);
                return null;
            }
            return new ScriptScraper(scraper);
        }

        public ScriptScraper(ScriptableScraper scriptableScraper)
        {
            scraper = scriptableScraper;
            name = scraper.Name;
            idString = scraper.ID.ToString();
            retrievesDetails = scraper.ScriptType.Contains("GameDetailsFetcher");
            retrievesCovers = scraper.ScriptType.Contains("GameCoverFetcher");
            retrievesScreens = scraper.ScriptType.Contains("GameScreenFetcher");
            retrievesFanart = scraper.ScriptType.Contains("GameFanartFetcher");
        }

        #endregion

        #region Properties

        public override string IdString
        {
            get { return idString; }
        }

        public override string Name
        {
            get { return name; }
        }

        public override bool RetrievesDetails 
        { 
            get { return retrievesDetails; } 
        }

        public override bool RetrievesCovers 
        { 
            get { return retrievesCovers; } 
        }

        public override bool RetrievesScreens 
        { 
            get { return retrievesScreens; } 
        }

        public override bool RetrievesFanart 
        { 
            get { return retrievesFanart; } 
        }

        #endregion

        public override List<ScraperResult> GetMatches(ScraperSearchParams searchParams)
        {
            Dictionary<string, string> paramList = new Dictionary<string, string>();
            if (searchParams.Term != null)
                paramList["search.title"] = searchParams.Term;
            if (searchParams.Platform != null)
                paramList["search.platform"] = searchParams.Platform;
            if (searchParams.PlatformId != null)
                paramList["search.platformid"] = searchParams.PlatformId;
            if (searchParams.Filename != null)
                paramList["search.filename"] = searchParams.Filename;

            List<ScraperResult> results = new List<ScraperResult>();
            Dictionary<string, string> scraperResults = scraper.Execute("search", paramList);
            if (scraperResults == null)
                return results;

            int count = 0; 
            string siteId;
            //loop and build results
            while (scraperResults.TryGetValue("game[" + count + "].site_id", out siteId))
            {
                string prefix = "game[" + count + "].";

                string system;
                if (scraperResults.TryGetValue(prefix + "system", out system))
                    system = cleanString(system);

                string title;
                if (scraperResults.TryGetValue(prefix + "title", out title))
                    title = cleanString(title);

                string year;
                if (scraperResults.TryGetValue(prefix + "yearmade", out year))
                    year = cleanString(year);

                string scoreStr;
                int score;
                if (!scraperResults.TryGetValue(prefix + "score", out scoreStr) || !int.TryParse(scoreStr, out score))
                    score = FuzzyStringComparer.Score(searchParams.Term, ScraperProvider.RemoveSpecialChars(title));

                results.Add(new ScraperResult(siteId, title, system, year, this, searchParams) { SearchDistance = score });
                count++;
            }

            return results;
        }

        public override ScraperGame GetDetails(ScraperResult result)
        {
            Dictionary<string, string> paramList = new Dictionary<string, string>();
            paramList["game.site_id"] = result.SiteId;
            Dictionary<string, string> results = scraper.Execute("get_details", paramList);

            if (results == null)
                return null;

            string grade, title, yearmade, company, description, genre;
            results.TryGetValue("game.grade", out grade);
            results.TryGetValue("game.title", out title);
            results.TryGetValue("game.yearmade", out yearmade);
            results.TryGetValue("game.company", out company);
            results.TryGetValue("game.description", out description);
            if (results.TryGetValue("game.genre", out genre))
            {
                genre = cleanString(genre);
                if (genre.StartsWith("|"))
                    genre = genre.Remove(0, 1);
            }

            return new ScraperGame(cleanString(title), cleanString(company), cleanString(yearmade), cleanString(grade), cleanString(description), genre);
        }

        public override List<string> GetCoverUrls(ScraperResult result)
        {
            Dictionary<string, string> imageResults = scraper.Execute("get_cover_art", getDetailsParams(result.SiteId));
            if (imageResults == null)
                return new List<string>();

            string baseUrl; 
            imageResults.TryGetValue("game.baseurl", out baseUrl);
            return getImageUrls(imageResults, baseUrl);
        }

        public override bool PopulateCovers(ScraperResult result, ScraperGame scraperGame)
        {
            Dictionary<string, string> imageResults = scraper.Execute("get_cover_art", getDetailsParams(result.SiteId));
            if (imageResults == null)
                return base.PopulateCovers(result, scraperGame);

            string baseUrl;
            imageResults.TryGetValue("game.baseurl", out baseUrl);

            bool hasFront = !string.IsNullOrEmpty(scraperGame.BoxFrontUrl);
            bool hasBack = !string.IsNullOrEmpty(scraperGame.BoxBackUrl);

            if (!hasFront)
            {
                string coverFront;
                if (imageResults.TryGetValue("game.cover.front", out coverFront) && !string.IsNullOrEmpty(coverFront))
                {
                    scraperGame.BoxFrontUrl = expandUrl(coverFront, baseUrl);
                    hasFront = true;
                }
            }

            if (!hasBack)
            {
                string coverBack;
                if (string.IsNullOrEmpty(scraperGame.BoxBackUrl) && imageResults.TryGetValue("game.cover.back", out coverBack) && !string.IsNullOrEmpty(coverBack))
                {
                    scraperGame.BoxBackUrl = expandUrl(coverBack, baseUrl);
                    hasBack = true;
                }
            }

            if (!hasFront || !hasBack)
            {
                List<string> urls = getImageUrls(imageResults, baseUrl);
                matchUrlToImageType(urls, scraperGame, ThumbSearchType.Covers);
            }

            return base.PopulateCovers(result, scraperGame);
        }

        public override List<string> GetScreenUrls(ScraperResult result)
        {
            Dictionary<string, string> imageResults = scraper.Execute("get_screenshots", getDetailsParams(result.SiteId));
            if (imageResults == null)
                return new List<string>();

            string baseUrl;
            imageResults.TryGetValue("game.baseurl", out baseUrl);
            return getImageUrls(imageResults, baseUrl);
        }

        public override bool PopulateScreens(ScraperResult result, ScraperGame scraperGame)
        {
            Dictionary<string, string> imageResults = scraper.Execute("get_screenshots", getDetailsParams(result.SiteId));
            if (imageResults == null)
                return base.PopulateScreens(result, scraperGame);

            string baseUrl;
            imageResults.TryGetValue("game.baseurl", out baseUrl);

            bool hasFront = !string.IsNullOrEmpty(scraperGame.TitleScreenUrl);
            bool hasBack = !string.IsNullOrEmpty(scraperGame.InGameUrl);

            if (!hasFront)
            {
                string titleScreen;
                if (imageResults.TryGetValue("game.screen.title", out titleScreen) && !string.IsNullOrEmpty(titleScreen))
                {
                    titleScreen = expandUrl(titleScreen, baseUrl);
                    hasFront = true;
                }
            }

            if (!hasBack)
            {
                string inGame;
                if (imageResults.TryGetValue("game.screen.ingame", out inGame) && !string.IsNullOrEmpty(inGame))
                {
                    inGame = expandUrl(inGame, baseUrl);
                    hasBack = true;
                }
            }

            if (!hasFront || !hasBack)
            {
                List<string> urls = getImageUrls(imageResults, baseUrl);
                matchUrlToImageType(urls, scraperGame, ThumbSearchType.Screens);
            }

            return base.PopulateScreens(result, scraperGame);
        }

        public override List<string> GetFanartUrls(ScraperResult result)
        {
            Dictionary<string, string> imageResults = scraper.Execute("get_fanart", getDetailsParams(result.SiteId));
            if (imageResults == null)
                return new List<string>();

            string baseUrl;
            imageResults.TryGetValue("game.baseurl", out baseUrl);
            return getImageUrls(imageResults, baseUrl);
        }

        public override bool PopulateFanart(ScraperResult result, ScraperGame scraperGame)
        {
            bool isPopulated = base.PopulateFanart(result, scraperGame);
            if (isPopulated)
                return true;

            List<string> urls = GetFanartUrls(result);
            if (urls.Count > 0 && !string.IsNullOrEmpty(urls[0]))
                scraperGame.FanartUrl = urls[0];

            return base.PopulateFanart(result, scraperGame);
        }

        List<string> getImageUrls(Dictionary<string, string> results, string baseUrl)
        {
            List<string> urls = new List<string>();
            string images;
            if (results.TryGetValue("game.images", out images))
            {
                string[] imageUrls = images.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                for (int x = 0; x < imageUrls.Length; x++)
                    if (!string.IsNullOrEmpty(imageUrls[x]))
                        urls.Add(expandUrl(imageUrls[x], baseUrl));
            }
            return urls;
        }

        void matchUrlToImageType(List<string> urls, ScraperGame scraperGame, ThumbSearchType thumbType)
        {
            string image1, image2;
            string[] tags;
            if (thumbType == ThumbSearchType.Covers)
            {
                image1 = scraperGame.BoxFrontUrl;
                image2 = scraperGame.BoxBackUrl;
                tags = new string[] { FRONT_IMAGE_TAG, BACK_IMAGE_TAG };
            }
            else
            {
                image1 = scraperGame.TitleScreenUrl;
                image2 = scraperGame.InGameUrl;
                tags = new string[] { TITLE_IMAGE_TAG, null };
            }

            bool found1 = !string.IsNullOrEmpty(image1);
            bool found2 = !string.IsNullOrEmpty(image2);

            bool checkTag1 = !string.IsNullOrEmpty(tags[0]);
            bool checkTag2 = !string.IsNullOrEmpty(tags[1]);

            for (int x = 0; x < urls.Count; x++)
            {
                string url = urls[x].ToLower();
                if (checkTag1 && !found1 && url.Contains(tags[0]))
                {
                    image1 = urls[x];
                    found1 = true;
                }
                else if (checkTag2 && !found2 && url.Contains(tags[1]))
                {
                    image2 = urls[x];
                    found2 = true;
                }
                else if (string.IsNullOrEmpty(image1))
                {
                    image1 = urls[x];
                }
                else if (string.IsNullOrEmpty(image2))
                {
                    image2 = urls[x];
                }

                if (found1 && found2)
                    break;
            }

            if (thumbType == ThumbSearchType.Covers)
            {
                scraperGame.BoxFrontUrl = image1;
                scraperGame.BoxBackUrl = image2;
            }
            else
            {
                scraperGame.TitleScreenUrl = image1;
                scraperGame.InGameUrl = image2;
            }
        }

        Dictionary<string, string> getDetailsParams(string siteId)
        {
            Dictionary<string, string> detailsParams = new Dictionary<string, string>();
            detailsParams["game.site_id"] = siteId;
            return detailsParams;
        }

        string expandUrl(string partialUrl, string baseUrl)
        {
            if (!string.IsNullOrEmpty(baseUrl) && !string.IsNullOrEmpty(partialUrl) && !partialUrl.ToLower().StartsWith("http://"))
                return baseUrl + partialUrl;
            return partialUrl;
        }
        
        string cleanString(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "";

            s = Regex.Replace(s, @"<br\s*/?\s*>", System.Environment.NewLine);
            s = Regex.Replace(s, "<[^>]*>", "");
            s = Regex.Replace(s, @"\s\s+", " ");
            return s.Trim();
        }
    }
}
