using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Emulators.Import
{
    enum ThumbSearchType
    {
        Covers,
        Screens,
        Fanart
    }

    public abstract class Scraper
    {
        public abstract string IdString { get; }
        public abstract string Name { get; }
        public abstract bool RetrievesDetails { get; }
        public abstract bool RetrievesCovers { get; }
        public abstract bool RetrievesScreens { get; }
        public abstract bool RetrievesFanart { get; }

        public override string ToString()
        {
            return Name;
        }

        public virtual List<ScraperResult> GetMatches(ScraperSearchParams searchParams)
        {
            return new List<ScraperResult>();
        }

        public virtual ScraperResult GetFirstMatch(ScraperSearchParams searchParams)
        {
            List<ScraperResult> results = GetMatches(searchParams);
            if (results != null && results.Count > 0)
            {
                results.Sort();
                return results[0];
            }
            return null;
        }

        public virtual ScraperGame GetDetails(ScraperResult result)
        {
            return null;
        }

        public virtual List<string> GetCoverUrls(ScraperResult result)
        {
            return new List<string>();
        }

        public virtual bool PopulateCovers(ScraperResult result, ScraperGame scraperGame) 
        {
            return !string.IsNullOrEmpty(scraperGame.BoxFrontUrl) && !string.IsNullOrEmpty(scraperGame.BoxBackUrl);
        }

        public virtual List<string> GetScreenUrls(ScraperResult result)
        {
            return new List<string>();
        }

        public virtual bool PopulateScreens(ScraperResult result, ScraperGame scraperGame) 
        {
            return !string.IsNullOrEmpty(scraperGame.TitleScreenUrl) && !string.IsNullOrEmpty(scraperGame.InGameUrl);
        }

        public virtual List<string> GetFanartUrls(ScraperResult result)
        {
            return new List<string>();
        }

        public virtual bool PopulateFanart(ScraperResult result, ScraperGame scraperGame) 
        {
            return !string.IsNullOrEmpty(scraperGame.FanartUrl);
        }
    }
}
