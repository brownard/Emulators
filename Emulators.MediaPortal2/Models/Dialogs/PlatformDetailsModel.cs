using Emulators.ImageHandlers;
using Emulators.PlatformImporter;
using MediaPortal.Common;
using MediaPortal.Common.Threading;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Workflow;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.MediaPortal2.Models.Dialogs
{
    class PlatformDetailsModel : ProgressDialogModel
    {
        IPlatformImporter importer;
        string platformStr;
        Emulator emulator;

        public PlatformDetailsModel()
        {
            importer = new TheGamesDBImporter();
        }

        public static void GetPlatformInfo(string platformStr, Emulator emulator)
        {
            IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
            PlatformDetailsModel instance = (PlatformDetailsModel)workflowManager.GetModel(Guids.PlatformDetailsModel);
            instance.Platform = platformStr;
            instance.Emulator = emulator;
            workflowManager.NavigatePushAsync(Guids.PlatformDetailsState);
        }

        public string Platform
        {
            get { return platformStr; }
            set { platformStr = value; }
        }

        public Emulator Emulator
        {
            get { return emulator; }
            set { emulator = value; }
        }

        public override Guid ModelId { get { return Guids.PlatformDetailsModel; } }

        protected override void DoTask()
        {
            SetProgress(string.Format("Looking up {0}", platformStr), 0);
            var platform = importer.GetPlatformByName(platformStr);
            if (platform != null)
            {
                SetProgress(string.Format("Retrieving info for {0}", platform.Name), 33);
                var platformInfo = importer.GetPlatformInfo(platform.Id);
                if (platformInfo != null)
                {
                    SetProgress(string.Format("Updating {0}", emulator.Title), 67);
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
