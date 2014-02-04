using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Emulators.Import
{
    class OfflineMameScraper : Scraper
    {
        public override string IdString
        {
            get { return "-1"; }
        }

        public override string Name
        {
            get { return "Mame Offline Scraper"; }
        }

        public override bool RetrievesDetails
        {
            get { return true; }
        }

        public override bool RetrievesCovers
        {
            get { return false; }
        }

        public override bool RetrievesScreens
        {
            get { return false; }
        }

        public override bool RetrievesFanart
        {
            get { return false; }
        }

        bool isInit = false;
        object initLock = new object();
        Dictionary<string, MameInfo> filenameLookup;
        Dictionary<string, List<MameInfo>> titleLookup;

        void init()
        {
            if (isInit)
                return;

            lock (initLock)
            {
                if (isInit)
                    return;

                filenameLookup = new Dictionary<string, MameInfo>();
                titleLookup = new Dictionary<string, List<MameInfo>>();

                bool success = true;
                string offlineInfo = "";
                try
                {
                    using (StreamReader reader = File.OpenText(@"")) //MAME synopsis RCB 201202\MAME.txt
                    {
                        offlineInfo = reader.ReadToEnd();
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError("Failed to read Mame offline info -", ex);
                    success = false;
                }

                if (success)
                {
                    string[] results = offlineInfo.Split(new[] { "*#*#*#*\nGame:" }, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 1; i < results.Length; i++)
                    {
                        MameInfo mameInfo;
                        if (MameInfo.TryCreate(results[i], out mameInfo))
                        {
                            filenameLookup[mameInfo.Filename] = mameInfo;
                            List<MameInfo> titleInfos;
                            if (titleLookup.TryGetValue(mameInfo.Title, out titleInfos))
                                titleInfos.Add(mameInfo);
                            else
                                titleLookup.Add(mameInfo.Title, new List<MameInfo>(new[] { mameInfo }));
                        }
                    }
                }
                isInit = true;
            }
        }

        public override List<ScraperResult> GetMatches(ScraperSearchParams searchParams)
        {
            if (!string.IsNullOrEmpty(searchParams.Platform) && searchParams.Platform != "Arcade")
                return base.GetMatches(searchParams);

            init();
            List<ScraperResult> results = new List<ScraperResult>();

            if (searchParams.Filename != null)
            {
                MameInfo fileMatch;
                if (filenameLookup.TryGetValue(searchParams.Filename, out fileMatch))
                    results.Add(createResult(fileMatch, searchParams));
            }

            List<MameInfo> titleMatches;
            if (titleLookup.TryGetValue(searchParams.Term, out titleMatches))
                foreach (MameInfo titleMatch in titleMatches)
                    results.Add(createResult(titleMatch, searchParams));

            return results;
        }

        public override ScraperGame GetDetails(ScraperResult result)
        {
            init();
            MameInfo mameInfo;
            if (!filenameLookup.TryGetValue(result.SiteId, out mameInfo))
                return null;

            return new ScraperGame(mameInfo.Title, mameInfo.Developer, mameInfo.Year, "0", mameInfo.Description, mameInfo.Genre);
        }

        ScraperResult createResult(MameInfo mameInfo, ScraperSearchParams searchParams)
        {
            return new ScraperResult(mameInfo.Filename, mameInfo.Title, "Arcade", mameInfo.Year, this, searchParams);
        }
    }

    class MameInfo
    {        
        public string Title { get; set; }
        public string Filename { get; set; }
        public string Year { get; set; }
        public string Genre { get; set; }
        public string Developer { get; set; }
        public string Description { get; set; }

        public static bool TryCreate(string text, out MameInfo mameInfo)
        {
            mameInfo = new MameInfo();
            int itemStart = 0;
            int itemEnd = text.IndexOf('\n');
            if(itemEnd < 0)
                return false;

            mameInfo.Title = text.Substring(itemStart, itemEnd).Trim();

            while (true)
            {
                itemEnd++;
                if (itemEnd >= text.Length)
                    break;
                itemStart = itemEnd;
                itemEnd = text.IndexOf('\n', itemStart);
                if (itemEnd < 0)
                    return false;

                string info = text.Substring(itemStart, itemEnd - itemStart);
                int tagEnd = info.IndexOf(':');
                if (tagEnd < 0)
                    continue;
                string tag = info.Remove(tagEnd).Trim();
                string val = info.Substring(tagEnd + 1).Trim();
                if (tag == "Genre")
                    mameInfo.Genre = val;
                else if (tag == "Release Year")
                    mameInfo.Year = val;
                else if (tag == "Developer")
                    mameInfo.Developer = val;
                else if (tag == "Game Filename")
                {
                    mameInfo.Filename = val;
                    itemEnd++;
                    if (itemEnd < text.Length)
                    {
                        int descriptionEnd = text.IndexOf("\n*#*#*#*", itemEnd);
                        if (descriptionEnd < 0)
                            descriptionEnd = text.Length;
                        mameInfo.Description = text.Substring(itemEnd, descriptionEnd - itemEnd).Trim();
                    }
                    break;
                }
            }

            return mameInfo.Filename != null;
        }
    }
}
