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
        public const string KEY_PLATFORM_STRING = "PlatformDetailsPlatformString";
        public const string KEY_EMULATOR = "PlatformDetailsEmulator";

        IPlatformImporter importer;
        string platformStr;
        Emulator emulator;

        public PlatformDetailsModel()
        {
            importer = new TheGamesDBImporter();
        }

        public static void GetPlatformInfo(string platformStr, Emulator emulator)
        {
            IDictionary<string, object> contextVariables = new Dictionary<string, object>();
            contextVariables[KEY_PLATFORM_STRING] = platformStr;
            contextVariables[KEY_EMULATOR] = emulator;
            IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
            workflowManager.NavigatePushAsync(Guids.PlatformDetailsState, new NavigationContextConfig() { AdditionalContextVariables = contextVariables });
        }

        public string Platform
        {
            get { return platformStr; }
        }

        public Emulator Emulator
        {
            get { return emulator; }
        }

        public override Guid ModelId { get { return Guids.PlatformDetailsModel; } }

        protected override void DoTask(NavigationContext context)
        {
            if (!getParameters(context))
                return;

            SetProgress(string.Format("Looking up {0}", platformStr), 0);
            var platform = importer.GetPlatformByName(platformStr);
            if (platform != null)
            {
                SetProgress(string.Format("Retrieving info for {0}", platform.Name), 33);
                var platformInfo = importer.GetPlatformInfo(platform.Id);
                if (platformInfo != null)
                {
                    System.Threading.Thread.Sleep(2000);
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

        bool getParameters(NavigationContext context)
        {
            platformStr = null;
            emulator = null;

            object parameter;
            if (!context.ContextVariables.TryGetValue(KEY_PLATFORM_STRING, out parameter))
                return false;
            platformStr = parameter as string;

            if (!context.ContextVariables.TryGetValue(KEY_EMULATOR, out parameter))
                return false;
            emulator = parameter as Emulator;

            return platformStr != null && emulator != null;
        }
    }
}