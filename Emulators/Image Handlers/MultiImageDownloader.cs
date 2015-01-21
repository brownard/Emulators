using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace Emulators.Image_Handlers
{
    class MultiImageDownloader
    {
        const string USER_AGENT = "Mozilla/5.0 (Windows NT 6.1; rv:6.0.1) Gecko/20100101 Firefox/6.0.1";

        volatile bool completed = false;
        IEnumerator<string> urls;
        
        public MultiImageDownloader(IEnumerable<string> urls)
        {
            this.urls = urls.GetEnumerator();
        }

        public bool IsCompleted
        {
            get { return completed; }
        }

        public event EventHandler<ImageDownloadedEventArgs> ImageDownloaded;
        protected virtual void OnImageDownloaded(ImageDownloadedEventArgs e)
        {
            var imageDownloaded = ImageDownloaded;
            if (imageDownloaded != null)
                imageDownloaded(this, e);
        }

        public event EventHandler Completed;
        protected virtual void OnCompleted()
        {
            var completed = Completed;
            if (completed != null)
                completed(this, EventArgs.Empty);
        }

        public void DownloadAsync()
        {
            if (completed)
                return;
            if (!urls.MoveNext())
            {
                completed = true;
                OnCompleted();
                return;
            }
            
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(urls.Current);
                request.UserAgent = USER_AGENT;
                request.BeginGetResponse(ar =>
                {
                    try
                    {
                        using (HttpWebResponse response = request.EndGetResponse(ar) as HttpWebResponse)
                            OnImageDownloaded(new ImageDownloadedEventArgs(Image.FromStream(response.GetResponseStream())));
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("Error downloading thumb from {0} - {1}", urls.Current, ex.Message);
                    }
                    DownloadAsync();
                }, null);
            }
            catch (Exception ex)
            {
                Logger.LogError("Error downloading thumb from {0} - {1}", urls.Current, ex.Message);
                DownloadAsync();
            }
        }

        public void Cancel()
        {
            completed = true;
        }
    }

    public class ImageDownloadedEventArgs : EventArgs
    {
        public ImageDownloadedEventArgs(Image image)
        {
            Image = image;
        }

        public Image Image { get; private set; }
    }
}
