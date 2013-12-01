using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Emulators.Import
{
    public class ScraperResult : IComparable<ScraperResult>
    {
        public string SiteId { get; set; }
        public string Title { get; set; }
        public string System { get; set; }
        public string Year { get; set; }
        public int SearchDistance { get; set; }
        public ScraperSearchParams SearchParams { get; set; }
        public Scraper DataProvider { get; set; }
        public int Priority { get; set; }

        public string DisplayMember
        {
            get
            {
                string dispStr = "";
                if (!string.IsNullOrEmpty(Year))
                {
                    dispStr += Year;
                }
                if (!string.IsNullOrEmpty(System))
                {
                    if (!string.IsNullOrEmpty(dispStr))
                        dispStr += ", ";
                    dispStr += System;
                }
                if (!string.IsNullOrEmpty(dispStr))
                    dispStr = " (" + dispStr + ")";

                return string.Format("{0}{1} [{2}]", Title, dispStr, DataProvider.Name);
            }
        }

        public override string ToString()
        {
            return DisplayMember;
        }

        public ScraperResult Self
        {
            get { return this; }
        }

        public ScraperResult(string siteId, string title, string system, string year, Scraper dataProvider, ScraperSearchParams searchParams)
        {
            SiteId = siteId == null ? "" : siteId;
            Title = title == null ? "" : title;
            System = system == null ? "" : system;
            Year = year == null ? "" : year;
            DataProvider = dataProvider;
            SearchParams = searchParams;
            SearchDistance = 0;
        }

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
    }
}
