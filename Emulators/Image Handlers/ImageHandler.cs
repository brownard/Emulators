using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Xml;
using System.Net;
using System.Drawing.Drawing2D;

namespace Emulators.ImageHandlers
{
    public class ImageHandler
    {
        public static Image NewImage(Image input)
        {
            if (input == null)
                return null;

            Image output = new Bitmap(input.Width, input.Height, PixelFormat.Format32bppPArgb);
            using (Graphics graphics = Graphics.FromImage(output))
                graphics.DrawImage(input, 0, 0, input.Width, input.Height);
            return output;
        }

        public static Image ResizeImage(Image input, double ratio, int maxThumbDimension = 0)
        {
            if (input == null)
                return null;

            int newWidth = input.Width;
            int newHeight = input.Height;
            if (newWidth > newHeight)
            {
                if (ratio > 0)
                    newHeight = (int)(input.Width / ratio);
                resizeDimensions(ref newWidth, ref newHeight, maxThumbDimension);
            }
            else
            {
                if (ratio > 0)
                    newWidth = (int)(input.Height * ratio);
                resizeDimensions(ref newHeight, ref newWidth, maxThumbDimension);
            }

            return newImage(input, newWidth, newHeight);
        }

        static Image newImage(Image input, int width, int height)
        {
            Image output = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
            using (Graphics graphics = Graphics.FromImage(output))
            {
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.DrawImage(input, 0, 0, width, height);
            }
            return output;
        }

        static void resizeDimensions(ref int largeDimension, ref int smallDimension, int max)
        {
            if (max > 0 && largeDimension > max)
            {
                double factor = (double)max / largeDimension;
                largeDimension = max;
                smallDimension = (int)(smallDimension * factor);
            }
        }

        public static Bitmap BitmapFromWeb(string url)
        {            
            using (HttpWebResponse response = getWebResponse(url))
            {
                if (response != null)
                {
                    try
                    {
                        return new Bitmap(response.GetResponseStream());
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("Error creating Bitmap from {0} - {1}", url, ex.Message);
                    }
                }
            }
            return null;
        }

        public static SafeImage SafeImageFromWeb(string url)
        {
            using (HttpWebResponse response = getWebResponse(url))
            {
                if (response != null)
                {
                    try
                    {
                        return new SafeImage(response.GetResponseStream());
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("Error creating SafeImage from {0} - {1}", url, ex.Message);
                    }
                }
            }
            return null;
        }

        static HttpWebResponse getWebResponse(string url)
        {
            if (string.IsNullOrEmpty(url))
                return null;

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.UserAgent = EmulatorsCore.USER_AGENT;
                return (HttpWebResponse)request.GetResponse();
            }
            catch (Exception ex)
            {
                Logger.LogError("Error getting web response from {0} - {1}", url, ex.Message);
                return null;
            }
        }

        public static BitmapDownloadResult BeginBitmapFromWeb(string url)
        {
            BitmapDownloadResult result = new BitmapDownloadResult();
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.UserAgent = EmulatorsCore.USER_AGENT;
                request.BeginGetResponse((asyncRes) =>
                {
                    SafeImage safeImage = null;
                    try
                    {
                        using (HttpWebResponse response = request.EndGetResponse(asyncRes) as HttpWebResponse)
                            safeImage = new SafeImage(response.GetResponseStream());
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("Error downloading thumb from {0} - {1}", url, ex.Message);
                    }
                    finally
                    {
                        result.Complete(safeImage);
                    }
                }, null);
            }
            catch (Exception ex)
            {
                Logger.LogError("Error downloading thumb from {0} - {1}", url, ex.Message);
                return null;
            }
            return result;
        }
    }

    public class BitmapDownloadResult : IDisposable
    {
        object syncRoot = new object();
        volatile bool cancelled = false;
        SafeImage safeImage = null;
        public SafeImage SafeImage
        {
            get { return safeImage; }
        }

        volatile bool isCompleted = false;
        public bool IsCompleted
        {
            get { return isCompleted; }
        }

        internal void Complete(SafeImage safeImage)
        {
            lock (syncRoot)
            {
                if (!cancelled)
                {
                    this.safeImage = safeImage;
                }
                else if (safeImage != null)
                {
                    try 
                    { 
                        safeImage.Dispose();
                    }
                    catch { }
                }
                isCompleted = true;
            }
        }

        public void Cancel()
        {
            lock (syncRoot)
            {
                Dispose();
                cancelled = true;
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            lock (syncRoot)
            {
                if (safeImage != null)
                {
                    try { safeImage.Dispose(); }
                    catch { }
                }
            }
        }

        #endregion
    }

}
