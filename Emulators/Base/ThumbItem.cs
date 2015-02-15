using Emulators.ImageHandlers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Emulators
{
    public abstract class ThumbItem : DBItem
    {
        public abstract string ThumbFolder { get; }

        public virtual bool HasGameArt 
        { 
            get { return false; } 
        }

        public virtual double AspectRatio 
        {
            get { return 0; }
        }

        public virtual ThumbItem DefaultThumbItem 
        { 
            get { return null; } 
        }

        public virtual void DeleteThumbs()
        {
            using (ThumbGroup thumbs = new ThumbGroup(this))
            {
                if (Directory.Exists(thumbs.ThumbPath))
                {
                    Logger.LogDebug("Deleting thumb folder '{0}'", thumbs.ThumbPath);
                    try
                    {
                        Directory.Delete(thumbs.ThumbPath, true);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogDebug("Failed to delete pre-existing thumb folder '{0}' - {1}", thumbs.ThumbPath, ex.Message);
                    }
                }
            }
        }
    }
}
