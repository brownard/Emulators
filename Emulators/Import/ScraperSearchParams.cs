using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Emulators.Import
{
    public class ScraperSearchParams
    {
        #region Static Members

        static Dictionary<string, string> platformIdLookup;
        static ScraperSearchParams()
        {
            platformIdLookup = new Dictionary<string, string>();
            foreach (System.Data.DataRow row in Dropdowns.GetSystems().Rows)
            {
                string platformId = row[0].ToString();
                if (platformId != "-1")
                    platformIdLookup[row[1].ToString()] = platformId;
            }
        }

        #endregion

        public string Term { get; set; }
        string platform = null;
        public string Platform 
        {
            get { return platform; }
            set
            {
                platform = value;
                if (!string.IsNullOrEmpty(platform))
                {
                    if (platformIdLookup.ContainsKey(platform))
                        PlatformId = platformIdLookup[platform];
                    else
                        PlatformId = "0";
                }
            }
        }

        public string PlatformId { get; private set; }

        string filename = "";
        public string Filename 
        {
            get { return filename; }
            set
            {
                if (value == null)
                    value = "";
                filename = value;
            }
        }
    }
}
