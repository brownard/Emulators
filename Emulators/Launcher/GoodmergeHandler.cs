using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Emulators.Launcher
{
    public static class GoodmergeHandler
    {
        const string GOODMERGE_EXTRACT_FOLDER = "Emulators2_GoodMerge_Extract_Folder";

        static object cleanupSync = new object();
        static bool cleanupDone;
        public static void CleanupExtractionDirectory()
        {
            lock (cleanupSync)
            {
                if (cleanupDone)
                    return;

                string extractionDirectory = null;
                try
                {
                    extractionDirectory = Path.Combine(Path.GetTempPath(), GOODMERGE_EXTRACT_FOLDER);
                    DirectoryInfo dir = new DirectoryInfo(extractionDirectory);
                    if (dir.Exists)
                        foreach (DirectoryInfo subDir in dir.GetDirectories())
                            subDir.Delete(true);
                }
                catch (Exception ex)
                {
                    Logger.LogError("GoodmergeHandler: Error cleaning extraction directory '{0}', {1}", extractionDirectory, ex);
                }
                cleanupDone = true;
            }
        }

        public static string GetExtractionDirectory(string archivePath)
        {
            CleanupExtractionDirectory();
            return Path.Combine(Path.GetTempPath(), GOODMERGE_EXTRACT_FOLDER, Path.GetFileNameWithoutExtension(archivePath));
        }

        public static int GetFileIndex(string selectedFile, List<string> files, List<string> goodmergeTags)
        {
            int index = files.IndexOf(selectedFile);
            if (index != -1)
                return index;
            index = 0;
            int priority = goodmergeTags.Count * 2;
            for (int x = 0; x < files.Count; x++)
            {
                selectedFile = files[x];
                for (int y = 0; y < goodmergeTags.Count; y++)
                {
                    if (priority > y * 2 && selectedFile.Contains(goodmergeTags[y]))
                    {
                        if (selectedFile.Contains("[!]"))
                            priority = y * 2;
                        else if (priority > y * 2 + 1)
                            priority = y * 2 + 1;
                        else
                            continue;
                        index = x;
                    }
                }
                if (priority < 1)
                    break;
            }
            return index;
        }
    }
}
