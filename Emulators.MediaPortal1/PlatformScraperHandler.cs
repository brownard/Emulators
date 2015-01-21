using Emulators.PlatformImporter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Emulators.MediaPortal1
{
    static class PlatformScraperHandler
    {        
        public static PlatformInfo GetPlatformInfo(string platform, IPlatformImporter platformImporter, Func<PlatformInfo, bool> completedDelegate)
        {
            return getPlatformInfo(platform, null, platformImporter, completedDelegate);
        }

        static PlatformInfo getPlatformInfo(string platform, Platform selectedPlatform, IPlatformImporter importer, Func<PlatformInfo, bool> completedDelegate)
        {
            PlatformInfo platformInfo = null;
            bool completed = false;

            BackgroundTaskHandler handler = new BackgroundTaskHandler();
            handler.ActionDelegate = () =>
            {
                handler.ExecuteProgressHandler("Looking up platforms...", 0);
                if (selectedPlatform == null)
                    selectedPlatform = importer.GetPlatformByName(platform);
                if (selectedPlatform != null)
                {
                    handler.ExecuteProgressHandler("Retrieving info for " + platform, 33);
                    platformInfo = importer.GetPlatformInfo(selectedPlatform.Id);
                    if (platformInfo != null)
                    {
                        handler.ExecuteProgressHandler("Updating " + platformInfo.Title, 67);
                        completed = completedDelegate == null ? true : completedDelegate(platformInfo);
                    }
                }
            };

            using (Conf_ProgressDialog progressDlg = new Conf_ProgressDialog(handler))
                progressDlg.ShowDialog();

            if (completed)
                return platformInfo;

            if (selectedPlatform == null)
            {
                var platforms = importer.GetPlatformList();
                if (platforms.Count != 0)
                {
                    using (Conf_EmuLookupDialog lookupDlg = new Conf_EmuLookupDialog(platforms))
                    {
                        if (lookupDlg.ShowDialog() == DialogResult.OK && lookupDlg.SelectedPlatform != null)
                            return getPlatformInfo(lookupDlg.SelectedPlatform.Name, lookupDlg.SelectedPlatform, importer, completedDelegate);
                    }
                }
            }
            else
            {
                MessageBox.Show("Error retrieving online info.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            return null;
        }
    }
}
