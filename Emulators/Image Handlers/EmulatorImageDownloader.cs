using Emulators.PlatformImporter;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;

namespace Emulators.Image_Handlers
{
    public class EmulatorImageDownloader : ImageDownloadHandler
    {
        IPlatformImporter platformImporter;
        AutoResetEvent currentTask = new AutoResetEvent(true);

        public event EventHandler<PlatformSearchCompletedEventArgs> SearchCompleted;
        protected virtual void OnSearchCompleted(PlatformSearchCompletedEventArgs e)
        {
            var searchCompleted = SearchCompleted;
            if (searchCompleted != null)
                searchCompleted(this, e);
        }

        public EmulatorImageDownloader(IPlatformImporter platformImporter)
        {
            this.platformImporter = platformImporter;
        }

        public void Search(string platform)
        {
            invokeAsync(() =>
            {
                var platforms = platformImporter.GetPlatformList();
                var selectedPlatform = platformImporter.GetPlatformByName(platform);
                OnSearchCompleted(new PlatformSearchCompletedEventArgs(platforms, selectedPlatform));
            });
        }

        public void DownloadImages(string platformId)
        {
            invokeAsync(() =>
            {
                PlatformInfo platformInfo = platformImporter.GetPlatformInfo(platformId);
                if (platformInfo != null)
                    BeginDownload(platformInfo.ImageUrls, platformInfo.FanartUrls);
            });
        }

        public override void CancelDownload()
        {
            invokeAsync(() =>
            {
                base.CancelDownload();
                currentTask.Close();
            });
        }

        void invokeAsync(Action action)
        {
            new Thread(() => 
            {
                try
                {
                    currentTask.WaitOne();
                    action();
                    currentTask.Set();
                }
                catch { }
            }) { IsBackground = true }
            .Start();
        }
    }

    public class PlatformSearchCompletedEventArgs : EventArgs
    {
        public PlatformSearchCompletedEventArgs(ReadOnlyCollection<Platform> platforms, Platform selectedPlatform)
        {
            Platforms = platforms;
            SelectedPlatform = selectedPlatform;
        }

        public ReadOnlyCollection<Platform> Platforms { get; private set; }
        public Platform SelectedPlatform { get; private set; }
    }
}
