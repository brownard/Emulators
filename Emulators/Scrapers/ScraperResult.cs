using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Emulators.Scrapers
{
    public class ScraperResult : IComparable<ScraperResult>
    {
        #region Properties
        
        public string SiteId { get; private set; }
        public string Title { get; private set; }
        public string System { get; private set; }
        public string Year { get; private set; }
        public string BoxFrontUrl { get; set; }
        public string BoxBackUrl { get; set; }
        public string TitleScreenUrl { get; set; }
        public string InGameUrl { get; set; }
        public string FanartUrl { get; set; }

        public int SearchDistance { get; set; }
        public ScraperSearchParams SearchParams { get; set; }
        public Scraper DataProvider { get; set; }
        public int Priority { get; set; }

        public string DisplayMember
        {
            get { return ToString(); }
        }

        #endregion

        #region Ctor

        public ScraperResult(string siteId, string title, string system, string year, Scraper dataProvider, ScraperSearchParams searchParams)
        {
            SiteId = siteId.EmptyIfNull();
            Title = title.EmptyIfNull();
            System = system.EmptyIfNull();
            Year = year.EmptyIfNull();
            DataProvider = dataProvider;
            SearchParams = searchParams;
            SearchDistance = 0;
        }

        #endregion

        #region Overrides

        public override string ToString()
        {
            string s = Year;
            if (System != string.Empty)
            {
                if (s != string.Empty)
                    s += ", ";
                s += System;
            }
            if (s != string.Empty)
                s = " (" + s + ")";

            return string.Format("{0}{1} [{2}]", Title, s, DataProvider.Name);
        }

        #endregion

        #region IComparable

        public int CompareTo(ScraperResult other)
        {
            if (other == null)
                return -1;
            if (this == other)
                return 0;

            int comp = this.SearchDistance.CompareTo(other.SearchDistance);
            if (comp == 0)
                comp = this.Priority.CompareTo(other.Priority);
            return comp;
        }

        #endregion
    }
}
