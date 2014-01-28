using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.IO;

namespace Emulators
{
    /// <summary>
    /// Holds all the Thumb info for an Emulator or Game and provides methods
    /// for updating/viewing or deleting
    /// </summary>
    public class ThumbGroup : IDisposable
    {
        #region Constants
        public const string THUMB_DIR_NAME = "Emulators 2";
        public const string EMULATOR_DIR_NAME = "Emulators";
        public const string GAME_DIR_NAME = "Games";

        public const string LOGO_NAME = "Logo";
        public const string BOX_FRONT_NAME = "BoxFront";
        public const string BOX_BACK_NAME = "BoxBack";
        public const string TITLESCREEN_NAME = "TitleScreenshot";
        public const string INGAME_NAME = "IngameScreenshot";
        public const string FANART_NAME = "Fanart";
        public const string MANUAL_NAME = "Manual";
        #endregion

        #region Utility Methods
        public static bool IsThumbFile(string path, out ThumbType thumbType)
        {
            thumbType = ThumbType.FrontCover;
            string filename;
            string extension;
            try
            {
                filename = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                extension = Path.GetExtension(path).ToLowerInvariant();
            }
            catch { return false; }
            if (extension == ".jpg" || extension == ".png")
            {
                if (filename == LOGO_NAME.ToLowerInvariant())
                {
                    thumbType = ThumbType.Logo;
                    return true;
                }
                else if (filename == BOX_FRONT_NAME.ToLowerInvariant())
                {
                    thumbType = ThumbType.FrontCover;
                    return true;
                }
                else if (filename == BOX_BACK_NAME.ToLowerInvariant())
                {
                    thumbType = ThumbType.BackCover;
                    return true;
                }
                else if (filename == TITLESCREEN_NAME.ToLowerInvariant())
                {
                    thumbType = ThumbType.TitleScreen;
                    return true;
                }
                else if (filename == INGAME_NAME.ToLowerInvariant())
                {
                    thumbType = ThumbType.InGameScreen;
                    return true;
                }
                else if (filename == FANART_NAME.ToLowerInvariant())
                {
                    thumbType = ThumbType.Fanart;
                    return true;
                }
            }
            return false;
        }

        public static bool IsManualFile(string path)
        {
            string filename;
            string extension;
            try
            {
                filename = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                extension = Path.GetExtension(path).ToLowerInvariant();
            }
            catch { return false; }
            if (extension == ".pdf" && filename == MANUAL_NAME.ToLowerInvariant())
                return true;
            return false;
        }
        #endregion

        ImageCodecInfo jpegCodec = null;
        EncoderParameters encoderParams = null;
        ThumbItem parentItem;
        double thumbAspect = 0;
                
        /// <summary>
        /// Initialises a new ThumbGroup with the thumbs of the specified parent
        /// </summary>
        /// <param name="parent">An Emulator or Game</param>
        public ThumbGroup(ThumbItem parent)
        {
            if (parent == null)
                throw new ArgumentException("The parent item cannot be null", "parent");

            string thumbFolder = parent.ThumbFolder;
            thumbAspect = parent.AspectRatio;
            parentItem = parent;
            lock (parent.SyncRoot)
            {
                if (parent.Id != null)
                    thumbPath = string.Format(@"{0}\{1}\{2}\{3}\", Emulators2Settings.Instance.ThumbDirectory, THUMB_DIR_NAME, thumbFolder, parent.Id);
            }

            //init Thumbs
            logo = new Thumb(ThumbType.Logo);
            frontCover = new Thumb(ThumbType.FrontCover);
            backCover = new Thumb(ThumbType.BackCover);
            titleScreen = new Thumb(ThumbType.TitleScreen);
            inGame = new Thumb(ThumbType.InGameScreen);
            fanart = new Thumb(ThumbType.Fanart);
            //load the paths/images
            loadThumbs();
        }

        #region Properties        

        string thumbPath;
        /// <summary>
        /// The location where the thumbs will be saved
        /// </summary>
        public string ThumbPath
        {
            get { return thumbPath; }
        }

        Thumb logo = null;
        public Thumb Logo
        {
            get { return logo; }
        }

        Thumb frontCover = null;
        public Thumb FrontCover
        {
            get { return frontCover; }
        }

        Thumb backCover = null;
        public Thumb BackCover
        {
            get { return backCover; }
        }

        Thumb titleScreen = null;
        public Thumb TitleScreen
        {
            get { return titleScreen; }
        }

        Thumb inGame = null;
        public Thumb InGame
        {
            get { return inGame; }
        }

        Thumb fanart = null;
        public Thumb Fanart
        {
            get { return fanart; }
        }

        string manualPath = null;
        public string ManualPath
        {
            get 
            {
                if (manualPath == null) //we haven't tried loading existing manual yet
                {
                    if (string.IsNullOrEmpty(thumbPath))
                    {
                        Logger.LogWarn("No thumb path found for '{0}'", parentItem.Title);
                        manualPath = "";
                    }
                    else
                    {
                        string lPath = thumbPath + MANUAL_NAME + ".pdf";
                        if (System.IO.File.Exists(lPath))
                            manualPath = lPath;
                        else
                            manualPath = "";
                    }
                }

                return manualPath; 
            }
            set 
            {
                if (string.IsNullOrEmpty(value))
                    value = "";
                else if (!value.ToLower().EndsWith(".pdf"))
                {
                    Logger.LogError("Unable to update {0} Manual, file must be a pdf.");
                    return;
                }
                manualPath = value; 
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Opens the Thumb Directory in Windows Explorer, 
        /// highlighting the selected thumb if it exists
        /// </summary>
        /// <param name="thumbType">The thumb to select</param>
        public void BrowseThumbs(ThumbType thumbType)
        {
            if (string.IsNullOrEmpty(thumbPath))
            {
                Logger.LogWarn("No thumb path found for '{0}'", parentItem.Title);
                return;
            }

            string file = "";
            //get file name
            switch (thumbType)
            {
                case ThumbType.Logo:
                    file = LOGO_NAME;
                    break;
                case ThumbType.FrontCover:
                    file = BOX_FRONT_NAME;
                    break;
                case ThumbType.BackCover:
                    file = BOX_BACK_NAME;
                    break;
                case ThumbType.TitleScreen:
                    file = TITLESCREEN_NAME;
                    break;
                case ThumbType.InGameScreen:
                    file = INGAME_NAME;
                    break;
                case ThumbType.Fanart:
                    file = FANART_NAME;
                    break;
            }
            
            string args = "";
            if (System.IO.File.Exists(thumbPath + file + ".jpg"))
            {
                //selected thumb exists, set arguments to highlight file
                args = "/select," + thumbPath + file + ".jpg";
            }
            else if (System.IO.File.Exists(thumbPath + file + ".png"))
            {
                //selected thumb exists, set arguments to highlight file
                args = "/select," + thumbPath + file + ".png";
            }
            else
            {
                //selected thumb doesn't exist, 
                //check if directory exists and create if necessary
                if (!System.IO.Directory.Exists(thumbPath))
                {
                    try
                    {
                        System.IO.Directory.CreateDirectory(thumbPath);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("Error creating thumb directory for {0} - {1}", parentItem.Title, ex.Message);
                        return;
                    }
                }
                //set args to just open directory
                args = thumbPath;
            }

            // launch Explorer with selected args
            using (Process proc = new Process())
            {
                proc.StartInfo = new ProcessStartInfo();
                proc.StartInfo.FileName = "explorer.exe";
                proc.StartInfo.Arguments = args;
                proc.Start();
            }
        }

        /// <summary>
        /// Returns the path to the specified thumb
        /// </summary>
        /// <param name="thumbType"></param>
        /// <returns></returns>
        public string GetThumbPath(ThumbType thumbType)
        {
            switch (thumbType)
            {
                case ThumbType.Logo:
                    return logo.Path;
                case ThumbType.FrontCover:
                    return frontCover.Path;
                case ThumbType.BackCover:
                    return backCover.Path;
                case ThumbType.TitleScreen:
                    return titleScreen.Path;
                case ThumbType.InGameScreen:
                    return inGame.Path;
                case ThumbType.Fanart:
                    return fanart.Path;
                default:
                    return null;
            }
        }

        public bool HasLocalThumb(ThumbType thumbType)
        {
            string path;
            return HasLocalThumb(thumbType, out path);
        }

        public bool HasLocalThumb(ThumbType thumbType, out string path)
        {
            path = GetThumbPath(thumbType);
            string extension;
            return isValidThumbPath(path, out extension);
        }

        /// <summary>
        /// Returns the Image for the specified thumb
        /// </summary>
        /// <param name="thumbType"></param>
        /// <returns></returns>
        public Image GetThumb(ThumbType thumbType)
        {
            switch (thumbType)
            {
                case ThumbType.Logo:
                    return logo.Image;
                case ThumbType.FrontCover:
                    return frontCover.Image;
                case ThumbType.BackCover:
                    return backCover.Image;
                case ThumbType.TitleScreen:
                    return titleScreen.Image;
                case ThumbType.InGameScreen:
                    return inGame.Image;
                case ThumbType.Fanart:
                    return fanart.Image;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Updates the specified thumb with the specified path
        /// </summary>
        /// <param name="thumbType"></param>
        /// <param name="thumbPath"></param>
        public void UpdateThumb(ThumbType thumbType, string thumbPath)
        {
            switch (thumbType)
            {
                case ThumbType.Logo:
                    logo.Path = thumbPath;
                    break;
                case ThumbType.FrontCover:
                    frontCover.Path = thumbPath;
                    break;
                case ThumbType.BackCover:
                    backCover.Path = thumbPath;
                    break;
                case ThumbType.TitleScreen:
                    titleScreen.Path = thumbPath;
                    break;
                case ThumbType.InGameScreen:
                    inGame.Path = thumbPath;
                    break;
                case ThumbType.Fanart:
                    fanart.Path = thumbPath;
                    break;
            }
        }

        /// <summary>
        /// Updates the specified thumb with the specified image
        /// </summary>
        /// <param name="thumbType"></param>
        /// <param name="thumb"></param>
        public void UpdateThumb(ThumbType thumbType, Image thumb)
        {
            switch (thumbType)
            {
                case ThumbType.Logo:
                    logo.Image = thumb;
                    break;
                case ThumbType.FrontCover:
                    frontCover.Image = thumb;
                    break;
                case ThumbType.BackCover:
                    backCover.Image = thumb;
                    break;
                case ThumbType.TitleScreen:
                    titleScreen.Image = thumb;
                    break;
                case ThumbType.InGameScreen:
                    inGame.Image = thumb;
                    break;
                case ThumbType.Fanart:
                    fanart.Image = thumb;
                    break;
            }
        }

        /// <summary>
        /// Saves all configured thumbs to thumb directory
        /// </summary>
        public void SaveAllThumbs()
        {
            SaveThumb(ThumbType.Logo);
            SaveThumb(ThumbType.FrontCover);
            SaveThumb(ThumbType.BackCover);
            SaveThumb(ThumbType.TitleScreen);
            SaveThumb(ThumbType.InGameScreen);
            SaveThumb(ThumbType.Fanart);
        }

        /// <summary>
        /// Save specified thumb
        /// </summary>
        public void SaveThumb(ThumbType thumbType)
        {
            if (string.IsNullOrEmpty(thumbPath))
            {
                Logger.LogWarn("No thumb path found for '{0}'", parentItem.Title);
                return;
            }

            Thumb thumbObject;
            string thumbName;
            bool shrink = true;
            bool isCover = false;
            //get file/friendly name and set resize option
            switch (thumbType)
            {
                case ThumbType.Logo:
                    thumbObject = logo;
                    thumbName = LOGO_NAME;
                    break;
                case ThumbType.FrontCover:
                    thumbObject = frontCover;
                    thumbName = BOX_FRONT_NAME;
                    isCover = true;
                    break;
                case ThumbType.BackCover:
                    thumbObject = backCover;
                    thumbName = BOX_BACK_NAME;
                    isCover = true;
                    break;
                case ThumbType.TitleScreen:
                    thumbObject = titleScreen;
                    thumbName = TITLESCREEN_NAME;
                    break;
                case ThumbType.InGameScreen:
                    thumbObject = inGame;
                    thumbName = INGAME_NAME;
                    break;
                case ThumbType.Fanart:
                    thumbObject = fanart;
                    thumbName = FANART_NAME;
                    shrink = false;
                    break;
                default:
                    return;
            }

            if (!thumbObject.NeedsUpdate)
                return;

            if (thumbObject.Image == null)
            {
                //only delete if filename is also empty, else there was a problem
                //loading the image but path may still be valid
                if (string.IsNullOrEmpty(thumbObject.Path))
                    deleteThumb(thumbPath, thumbName);
                else
                    Logger.LogError("Unable to save {0} for {1} - error loading path '{2}'", thumbType, parentItem.Title, thumbObject.Path);
            }
            else
            {
                saveThumb(thumbObject, thumbPath + thumbName, isCover, shrink);
            }
            thumbObject.NeedsUpdate = false;
        }

        void saveThumb(Thumb thumbObject, string savePathWithoutExt, bool isCover, bool shrink)
        {
            int maxThumbDimension = 0;
            if (shrink)
            {
                maxThumbDimension = Options.Instance.GetIntOption("maxthumbdimension");
                if (maxThumbDimension < 0 || (thumbObject.Image.Width <= maxThumbDimension && thumbObject.Image.Height <= maxThumbDimension))
                    maxThumbDimension = 0;
            }

            string newPath;
            string extension;
            if (!isCover && maxThumbDimension == 0 && isValidThumbPath(thumbObject.Path, out extension))
            {
                newPath = savePathWithoutExt + extension;
                if (!copyThumbFromFile(thumbObject.Path, newPath))
                    return;
            }
            else
            {
                newPath = savePathWithoutExt + ".png";
                double aspectRatio = isCover ? thumbAspect : 0;
                if (!copyThumbFromImage(thumbObject.Image, aspectRatio, maxThumbDimension, newPath))
                    return;
            }

            RemoveAlternateThumb(newPath);
            thumbObject.Path = newPath;
        }

        bool copyThumbFromFile(string currentPath, string newPath)
        {
            if (currentPath != newPath)
            {
                try 
                {
                    if (!Directory.Exists(thumbPath))
                        Directory.CreateDirectory(thumbPath);
                    File.Copy(currentPath, newPath, true);
                    return true;
                }
                catch (Exception ex) 
                { 
                    Logger.LogError("Error copying '{0}' to '{1}' - {2}", currentPath, newPath, ex.Message);
                }
            }
            return false;
        }

        bool copyThumbFromImage(Image image, double aspectRatio, int maxThumbDimension, string newPath)
        {
            initImageEncoder();
            try
            {
                if (!Directory.Exists(thumbPath))
                    Directory.CreateDirectory(thumbPath);
                if (aspectRatio > 0 || maxThumbDimension > 0)
                    image = ImageHandler.ResizeImage(image, aspectRatio, maxThumbDimension);
                image.Save(newPath, jpegCodec, encoderParams);
            }
            catch (Exception ex)
            {
                Logger.LogError("Error saving image to '{0}' - {1}", newPath, ex.Message);
                return false;
            }
            return true;
        }

        void deleteThumb(string directory, string thumbName)
        {
            try { File.Delete(Path.Combine(directory, thumbName + ".jpg")); }
            catch { }
            try { File.Delete(Path.Combine(directory, thumbName + ".png")); }
            catch { }
        }

        public void SaveManual()
        {
            if (string.IsNullOrEmpty(thumbPath))
            {
                Logger.LogWarn("No thumb path found for '{0}'", parentItem.Title);
                return;
            }

            string savePath = thumbPath + MANUAL_NAME + ".pdf"; //destination dir
            string lPath = ManualPath; //initialise property and get configured manual path
            if (lPath == savePath) //configured manual is already in destination dir
                return;

            if (lPath == "") //delete manual
            {
                try { System.IO.File.Delete(savePath); }
                catch { }

                return;
            }

            //if configured path exists and is a pdf, copy to destination dir
            if (lPath.ToLower().EndsWith(".pdf") && System.IO.File.Exists(lPath))
            {
                try
                {
                    if (!Directory.Exists(thumbPath))
                        Directory.CreateDirectory(thumbPath);
                    System.IO.File.Copy(lPath, savePath, true);
                }
                catch (Exception ex)
                {
                    Logger.LogError("Error copying new Manual for {1} - {2}", parentItem.Title, ex.Message);
                    return;
                }
            }

        }

        /// <summary>
        /// Opens the specified thumb in the default image viewer
        /// </summary>
        /// <param name="thumbType"></param>
        public void LaunchThumb(ThumbType thumbType)
        {
            Thumb thumb = null;
            //get specified thumb
            switch (thumbType)
            {
                case ThumbType.Logo:
                    thumb = logo;
                    break;
                case ThumbType.FrontCover:
                    thumb = frontCover;
                    break;
                case ThumbType.BackCover:
                    thumb = backCover;
                    break;
                case ThumbType.TitleScreen:
                    thumb = titleScreen;
                    break;
                case ThumbType.InGameScreen:
                    thumb = inGame;
                    break;
                case ThumbType.Fanart:
                    thumb = fanart;
                    break;
            }

            string thumbPath;
            //path is in local file system use that
            if (!string.IsNullOrEmpty(thumb.Path) && !thumb.Path.ToLower().StartsWith("http://"))
                thumbPath = thumb.Path;
            //else if we have an image, save a temp image and use temp path
            else if (thumb.Image != null)
                thumbPath = getTempThumbPath(thumb.Image);
            else
                return; //no path or image

            if (thumbPath != null)
            {
                //open selected path
                using (Process proc = new Process())
                {
                    proc.StartInfo = new ProcessStartInfo(thumbPath);
                    proc.Start();
                }
            }
        }
        
        #endregion

        #region Private Methods

        //initialise image encoder settings, called on first image save
        void initImageEncoder()
        {
            if (jpegCodec == null)
            {
                ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
                for (int x = 0; x < codecs.Length; x++)
                {
                    if (codecs[x].MimeType == "image/png")
                        jpegCodec = codecs[x];
                }
                if (jpegCodec == null)
                {
                    Logger.LogError("Unable to locate the PNG codec");
                }
                encoderParams = new EncoderParameters(1);
                encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 85L);
            }
        }

        //loads all configured thumbs for the specified parent
        void loadThumbs()
        {
            fanart.Path = getThumbPath(ThumbType.Fanart);
            fanart.NeedsUpdate = false;
            if (parentItem.HasGameArt)
            {
                frontCover.Path = getThumbPath(ThumbType.FrontCover);
                frontCover.NeedsUpdate = false;
                backCover.Path = getThumbPath(ThumbType.BackCover);
                backCover.NeedsUpdate = false;
                titleScreen.Path = getThumbPath(ThumbType.TitleScreen);
                titleScreen.NeedsUpdate = false;
                inGame.Path = getThumbPath(ThumbType.InGameScreen);
                inGame.NeedsUpdate = false;
            }
            else
            {
                logo.Path = getThumbPath(ThumbType.Logo);
                logo.NeedsUpdate = false;
            }
        }

        static bool isValidThumbPath(string path)
        {
            string ext;
            return isValidThumbPath(path, out ext);
        }
        /// <summary>
        /// Checks if the specified image is in the local file system
        /// and is a supported image type.
        /// </summary>
        /// <param name="path">The full path to check</param>
        /// <param name="ext">If true this will be updated with the image extension</param>
        /// <returns></returns>
        static bool isValidThumbPath(string path, out string ext)
        {
            ext = null;
            if (path == null)
                return false;
            if (path.ToLower().StartsWith("http://") || !System.IO.File.Exists(path))
                return false;

            if (path.ToLower().EndsWith(".png"))
            {
                ext = ".png";
                return true;
            }
            if (path.ToLower().EndsWith(".jpg"))
            {
                ext = ".jpg";
                return true;
            }

            return false;
        }

        string getThumbPath(ThumbType thumbType)
        {
            if (string.IsNullOrEmpty(thumbPath))
            {
                Logger.LogWarn("No thumb path found for '{0}'", parentItem.Title);
                return null;
            }
            string path = thumbPath;
            switch (thumbType)
            {
                case ThumbType.Logo:
                    path += LOGO_NAME;
                    break;
                case ThumbType.FrontCover:
                    path += BOX_FRONT_NAME;
                    break;
                case ThumbType.BackCover:
                    path += BOX_BACK_NAME;
                    break;
                case ThumbType.TitleScreen:
                    path += TITLESCREEN_NAME;
                    break;
                case ThumbType.InGameScreen:
                    path += INGAME_NAME;
                    break;
                case ThumbType.Fanart:
                    path += FANART_NAME;
                    break;
            }

            if (System.IO.File.Exists(path + ".png"))
                return path + ".png";
            if (System.IO.File.Exists(path + ".jpg"))
                return path + ".jpg";
            return null;
        }

        //save the image to temp directory and return temp image path
        static string getTempThumbPath(Image thumb)
        {
            if (thumb == null)
                return null;

            //create unique temp save path
            string savePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "myEmulators2." + Guid.NewGuid().ToString() + ".bmp");
            try
            {
                thumb.Save(savePath, ImageFormat.Bmp);
            }
            catch(Exception ex)
            {
                Logger.LogError("Error saving temp image - {0}", ex.Message);
                return null;
            }
            return savePath;
        }

        //if savePath is jpg, remove png and vica versa
        static void RemoveAlternateThumb(string savePath)
        {            
            if(savePath == null || savePath.Length <= 4) //invalid path
                return;

            //get path without extension
            string pathWithoutExtension = savePath.Substring(0, savePath.Length - 4);
            //get extension
            string extension = savePath.Substring(savePath.Length - 4);

            //get alternate path
            string delPath = null;
            if (extension.ToLower() == ".jpg")
            {
                delPath = pathWithoutExtension + ".png";
            }
            else if (extension.ToLower() == ".png")
            {
                delPath = pathWithoutExtension + ".jpg";
            }

            if (delPath == null) //image is neither jpg or png
                return;

            try
            {
                System.IO.File.Delete(delPath);
            }
            catch { }
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            if (logo != null)
                logo.Dispose();
            if (frontCover != null)
                frontCover.Dispose();
            if (backCover != null)
                backCover.Dispose();
            if (titleScreen != null)
                titleScreen.Dispose();
            if (inGame != null)
                inGame.Dispose();
            if (fanart != null)
                fanart.Dispose();
        }

        #endregion

        public string FrontCoverDefaultPath
        {
            get
            {
                string path = null;
                if (parentItem.HasGameArt)
                {
                    if (isValidThumbPath(frontCover.Path))
                        path = frontCover.Path;
                }
                else if (isValidThumbPath(logo.Path))
                {
                    path = logo.Path;
                }

                if (string.IsNullOrEmpty(path) && parentItem.DefaultThumbItem != null)
                {
                    using (ThumbGroup defaultThumbs = new ThumbGroup(parentItem.DefaultThumbItem))
                        path = defaultThumbs.FrontCoverDefaultPath;
                }
                return path;
            }
        }
        /// <summary>
        /// Returns the fanart path. If Parent is
        /// a Game and no fanart is configured this will
        /// return the parent Emulator fanart path.
        /// </summary>
        public string FanartDefaultPath 
        {
            get
            {
                string path = null;
                //check if we have a local reference
                if (isValidThumbPath(fanart.Path))
                    path = fanart.Path;
                //if not, load parent ThumbGroup
                if (string.IsNullOrEmpty(path) && parentItem.DefaultThumbItem != null)
                {
                    using (ThumbGroup defaultThumbs = new ThumbGroup(parentItem.DefaultThumbItem))
                        path = defaultThumbs.FanartDefaultPath;
                }
                return path;
            }
        }
    }
}
