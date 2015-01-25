using SharpCompress.Archive;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Emulators.Launcher
{
    public class SharpCompressExtractor : IDisposable
    {
        static ConcurrentDictionary<string, string> cache = new ConcurrentDictionary<string, string>();
        public static List<string> ViewFiles(string archivePath)
        {
            using (SharpCompressExtractor extractor = new SharpCompressExtractor(archivePath))
            {
                extractor.Init();
                return extractor.Files;
            }
        }

        string archivePath;
        FileStream fileStream;
        IArchive extractor;

        public event EventHandler<ExtractionEventArgs> ExtractionProgress;
        protected virtual void OnExtractionProgress(ExtractionEventArgs e)
        {
            var extractionProgress = ExtractionProgress;
            if (extractionProgress != null)
                extractionProgress(this, e);
        }

        public event EventHandler ExtractionComplete;
        protected virtual void OnExtractionComplete()
        {
            var extractionComplete = ExtractionComplete;
            if (extractionComplete != null)
                extractionComplete(this, EventArgs.Empty);
        }

        public SharpCompressExtractor(string archivePath)
        {
            this.archivePath = archivePath;
        }

        public List<string> Files
        {
            get;
            private set;
        }

        public void Init()
        {
            try
            {
                fileStream = File.OpenRead(archivePath);
                extractor = ArchiveFactory.Open(fileStream, SharpCompress.Common.Options.None);
                if (extractor.Entries != null)
                    Files = extractor.Entries.Where(e => !e.IsDirectory).Select(e => e.FilePath).ToList();
            }
            catch (Exception ex)
            {
                Logger.LogError("Extractor: Failed top open archive '{0}', {1}", archivePath, ex);
            }
        }

        public string Extract(string archiveFile, string extractionDir)
        {
            if (extractor == null || extractor.Entries == null)
                return null;

            string outputPath;
            if (cache.TryGetValue(archiveFile + "|" + extractionDir, out outputPath))
                return outputPath;

            IArchiveEntry entry = extractor.Entries.FirstOrDefault(a => a.FilePath == archiveFile);
            if (entry != null)
            {
                outputPath = Path.Combine(extractionDir, entry.FilePath);
                try
                {
                    if (!Directory.Exists(extractionDir))
                        Directory.CreateDirectory(extractionDir);

                    long totalSize = entry.Size;
                    extractor.CompressedBytesRead += (sender, e) =>
                    {
                        int perc = (int)((e.CurrentFilePartCompressedBytesRead * 100) / totalSize);
                        OnExtractionProgress(new ExtractionEventArgs(perc));
                    };
                    extractor.EntryExtractionEnd += (sender, e) =>
                    {
                        OnExtractionComplete();
                    };
                    entry.WriteToFile(outputPath);
                    cache.TryAdd(archiveFile + "|" + extractionDir, outputPath);
                    return outputPath;
                }
                catch (Exception ex)
                {
                    Logger.LogError("Extractor: Failed to extract '{0}' to '{1}', {2}", entry.FilePath, outputPath, ex);
                }
            }
            return null;
        }

        public void Dispose()
        {
            if (extractor != null)
            {
                extractor.Dispose();
                extractor = null;
            }
            if (fileStream != null)
            {
                fileStream.Dispose();
                fileStream = null;
            }
        }
    }

    public class ExtractionEventArgs : EventArgs
    {
        public ExtractionEventArgs(int percent)
        {
            Percent = percent;
        }

        public int Percent { get; private set; }
    }
}
