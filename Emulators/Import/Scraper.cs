using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Emulators.Import
{
    public abstract class Scraper
    {
        public abstract string IdString { get; }
        public abstract string Name { get; }
        public abstract bool RetrievesDetails { get; }
        public abstract bool RetrievesCovers { get; }
        public abstract bool RetrievesScreens { get; }
        public abstract bool RetrievesFanart { get; }

        public virtual bool IsReady { get { return true; } }

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

        public virtual List<string> GetCoverUrls(ScraperResult result, out string coverFront, out string coverBack)
        {
            coverFront = null;
            coverBack = null;
            return new List<string>();
        }

        public virtual List<string> GetScreenUrls(ScraperResult result, out string titleScreen, out string inGame)
        {
            titleScreen = null;
            inGame = null;
            return new List<string>();
        }

        public virtual List<string> GetFanartUrls(ScraperResult result, out string fanart)
        {
            fanart = null;
            return new List<string>();
        }
    }
}
