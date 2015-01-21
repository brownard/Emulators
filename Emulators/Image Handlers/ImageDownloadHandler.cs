using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Emulators.Image_Handlers
{
    public class ImageDownloadHandler
    {
        MultiImageDownloader primaryDownloader;
        MultiImageDownloader secondaryDownloader;

        public event EventHandler ImageDownloadCompleted;
        protected virtual void OnImageDownloadCompleted()
        {
            var imageDownloadCompleted = ImageDownloadCompleted;
            if (imageDownloadCompleted != null)
                imageDownloadCompleted(this, EventArgs.Empty);
        }

        public event EventHandler<ImageDownloadedEventArgs> PrimaryImageDownloaded;
        protected virtual void OnPrimaryImageDownloaded(ImageDownloadedEventArgs e)
        {
            var imageDownloaded = PrimaryImageDownloaded;
            if (imageDownloaded != null)
                imageDownloaded(this, e);
        }

        public event EventHandler<ImageDownloadedEventArgs> SecondaryImageDownloaded;
        protected virtual void OnSecondaryImageDownloaded(ImageDownloadedEventArgs e)
        {
            var imageDownloaded = SecondaryImageDownloaded;
            if (imageDownloaded != null)
                imageDownloaded(this, e);
        }

        public void BeginDownload(IEnumerable<string> primaryUrls, IEnumerable<string> secondaryUrls)
        {
            cancelDownload();
            primaryDownloader = new MultiImageDownloader(primaryUrls);
            primaryDownloader.ImageDownloaded += imageDownloader_ImageDownloaded;
            primaryDownloader.Completed += imageDownloader_Completed;
            primaryDownloader.DownloadAsync();
            secondaryDownloader = new MultiImageDownloader(secondaryUrls);
            secondaryDownloader.ImageDownloaded += imageDownloader_ImageDownloaded;
            secondaryDownloader.Completed += imageDownloader_Completed;
            secondaryDownloader.DownloadAsync();
        }

        public virtual void CancelDownload()
        {
            cancelDownload();
        }

        void cancelDownload()
        {
            if (primaryDownloader != null)
            {
                primaryDownloader.ImageDownloaded -= imageDownloader_ImageDownloaded;
                primaryDownloader.Completed -= imageDownloader_Completed;
                primaryDownloader.Cancel();
            }
            if (secondaryDownloader != null)
            {
                secondaryDownloader.ImageDownloaded -= imageDownloader_ImageDownloaded;
                secondaryDownloader.Completed -= imageDownloader_Completed;
                secondaryDownloader.Cancel();
            }
        }

        void imageDownloader_ImageDownloaded(object sender, ImageDownloadedEventArgs e)
        {
            if (sender == primaryDownloader)
                OnPrimaryImageDownloaded(e);
            else
                OnSecondaryImageDownloaded(e);
        }

        void imageDownloader_Completed(object sender, EventArgs e)
        {
            if (primaryDownloader.IsCompleted && secondaryDownloader.IsCompleted)
                OnImageDownloadCompleted();
        }
    }
}
