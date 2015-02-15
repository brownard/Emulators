﻿using Emulators.ImageHandlers;
using Emulators.PlatformImporter;
using MediaPortal.Common;
using MediaPortal.Common.Threading;
using MediaPortal.UI.Presentation.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.MediaPortal2.Models.Dialogs
{
    class PlatformDetailsDialog
    {
        const string HEADER = "[Emulators.Dialogs.PlatformImporter]";

        IPlatformImporter importer = new TheGamesDBImporter();
        string platformStr;
        Emulator emulator;

        public PlatformDetailsDialog(string platformStr, Emulator emulator)
        {
            this.platformStr = platformStr;
            this.emulator = emulator;
        }

        public void GetPlatformInfo()
        {
            ProgressDialogModel.ShowDialog(HEADER, taskDelegate);
        }

        void taskDelegate(ProgressDialogModel progressDlg)
        {
            progressDlg.SetProgress(string.Format("Looking up {0}", platformStr), 0);
            var platform = importer.GetPlatformByName(platformStr);
            if (platform != null)
            {
                progressDlg.SetProgress(string.Format("Retrieving info for {0}", platform.Name), 33);
                var platformInfo = importer.GetPlatformInfo(platform.Id);
                if (platformInfo != null)
                {
                    progressDlg.SetProgress(string.Format("Updating {0}", emulator.Title), 67);
                    emulator.Title = platformInfo.Title;
                    emulator.Developer = platformInfo.Developer;
                    emulator.Description = platformInfo.GetDescription();

                    using (ThumbGroup thumbGroup = new ThumbGroup(emulator))
                    {
                        using (SafeImage image = ImageHandler.SafeImageFromWeb(platformInfo.LogoUrl))
                        {
                            if (image != null)
                            {
                                thumbGroup.Logo.SetSafeImage(image.Image);
                                thumbGroup.SaveThumb(ThumbType.Logo);
                            }
                        }
                        using (SafeImage image = ImageHandler.SafeImageFromWeb(platformInfo.FanartUrl))
                        {
                            if (image != null)
                            {
                                thumbGroup.Fanart.SetSafeImage(image.Image);
                                thumbGroup.SaveThumb(ThumbType.Fanart);
                            }
                        }
                    }
                    emulator.Commit();
                }
            }
        }
    }
}
