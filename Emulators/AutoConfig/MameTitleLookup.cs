using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Emulators.AutoConfig
{
    class MameTitleLookup : IGameTitleLookup
    {
        const string MAME_LIST_NAME = "mamelist.txt";

        object syncRoot = new object();
        Dictionary<string, string> nameLookup;

        public string GetTitle(string input, string platform)
        {
            if (platform == "Arcade")
            {
                init();
                string result;
                if (nameLookup.TryGetValue(input, out result))
                    return result;
            }
            return input;
        }

        void init()
        {
            lock (syncRoot)
            {
                if (nameLookup != null)
                    return;

                nameLookup = new Dictionary<string, string>();
                try
                {
                    FileInfo mameList = new FileInfo(Path.Combine(EmulatorsCore.DataPath, MAME_LIST_NAME));
                    using (StreamReader reader = new StreamReader(mameList.OpenRead()))
                    {
                        reader.ReadLine();
                        string item;
                        while ((item = reader.ReadLine()) != null)
                        {
                            int index = item.IndexOf(" ");
                            if (index > 0)
                            {
                                string name = item.Substring(index + 1).Trim(' ', '"');
                                nameLookup[item.Remove(index)] = name;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError("Error reading {0} - {1}", MAME_LIST_NAME, ex.Message);
                }
            }
        }
    }
}
