using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Emulators.Import
{
    class MameNameHandler
    {
        static object instanceLock = new object();
        static MameNameHandler instance;
        public static MameNameHandler Instance
        {
            get
            {
                if (instance == null)
                    lock (instanceLock)
                        if (instance == null)
                            instance = new MameNameHandler();
                return instance;
            }
        }
        
        const string RESOURCE_NAME = "Emulators.Data.mamelist.txt";
        Dictionary<string, string> nameLookup;
        
        public MameNameHandler()
        {
            nameLookup = new Dictionary<string, string>();
            using (StreamReader reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(RESOURCE_NAME)))
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

        public string GetName(string filename)
        {
            string result;
            if (nameLookup.TryGetValue(filename, out result))
                return result;
            return filename;
        }
    }
}
