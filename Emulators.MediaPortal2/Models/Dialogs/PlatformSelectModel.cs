﻿using MediaPortal.Common.Commands;
using MediaPortal.UI.Presentation.DataObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.MediaPortal2.Models.Dialogs
{
    public class PlatformSelectModel : ListDialogBase
    {
        const string KEY_PLATFORM = "Platform";

        public PlatformSelectModel()
        {
            items = new ItemsList();
            var platforms = Dropdowns.GetPlatformList();
            for (int i = -1; i < platforms.Count; i++)
            {
                string platformName = i < 0 ? "" : platforms[i].Name;
                ListItem item = new ListItem(Consts.KEY_NAME, platformName)
                {
                    Command = new MethodDelegateCommand(() => UpdatePlatform(platformName))
                };
                item.AdditionalProperties[KEY_PLATFORM] = platformName;
                items.Add(item);
            }
        }

        protected void UpdatePlatform(string platform)
        {
            var model = ConfigureEmulatorModel.Instance();
            model.Platform = platform;
        }

        protected override void UpdateSelectedFlag()
        {
            var model = ConfigureEmulatorModel.Instance();
            string currentPlatform = model.Platform;
            foreach (ListItem item in items)
            {
                object platform;
                if (item.AdditionalProperties.TryGetValue(KEY_PLATFORM, out platform) && (string)platform == currentPlatform)
                {
                    item.Selected = true;
                    break;
                }
            }
        }
    }
}
