using Emulators.AutoConfig;
using Emulators.MediaPortal1;
using Emulators.PlatformImporter;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Emulators
{
    internal partial class Wzd_NewEmu_Info : WzdPanel
    {
        Wzd_NewEmu_Main parent;
        IPlatformImporter platformImporter;
        public Wzd_NewEmu_Info(Wzd_NewEmu_Main parent, IPlatformImporter platformImporter)
        {
            InitializeComponent();
            platformComboBox.DisplayMember = "Name";
            var platforms = Dropdowns.GetPlatformList();
            foreach (Platform platform in platforms)
                platformComboBox.Items.Add(platform);
            txt_Title.Text = "New Emulator";
            this.parent = parent;
            this.platformImporter = platformImporter;
        }

        bool firstLoad = true;
        public override void UpdatePanel()
        {
            if (!string.IsNullOrEmpty(parent.NewEmulator.Title))
                txt_Title.Text = parent.NewEmulator.Title;

            platformComboBox.Text = parent.NewEmulator.Platform;
            //int index = platformComboBox.FindStringExact(parent.NewEmulator.Platform);
            //if (index > -1)
            //    platformComboBox.SelectedIndex = index;

            EmuAutoConfig.SetupAspectDropdown(thumbAspectComboBox, parent.NewEmulator.CaseAspect);

            if (firstLoad)
            {
                firstLoad = false;
                if (platformComboBox.SelectedIndex > 0)
                    updateEmuInfo();
            }
        }

        public override bool Next()
        {
            parent.NewEmulator.Title = txt_Title.Text;
            parent.NewEmulator.Platform = platformComboBox.Text;
            parent.NewEmulator.Developer = txt_company.Text;
            parent.NewEmulator.Description = txt_description.Text;
            parent.NewEmulator.Grade = (int)gradeUpDown.Value;
            parent.NewEmulator.SetCaseAspect(thumbAspectComboBox.Text);

            int year;
            if (!int.TryParse(txt_yearmade.Text, out year))
                year = 0;
            parent.NewEmulator.Year = year;
            return true;
        }

        public override bool Back()
        {
            return true;
        }

        private void updateInfoButton_Click(object sender, EventArgs e)
        {
            updateEmuInfo();
        }

        void updateEmuInfo()
        {
            PlatformInfo platformInfo = PlatformScraperHandler.GetPlatformInfo(platformComboBox.Text, platformImporter, p =>
                {
                    if (parent.Logo != null)
                    {
                        parent.Logo.Dispose();
                        parent.Logo = null;
                    }
                    if (parent.Fanart != null)
                    {
                        parent.Fanart.Dispose();
                        parent.Fanart = null;
                    }
                    parent.Logo = ImageHandler.BitmapFromWeb(p.LogoUrl);
                    parent.Fanart = ImageHandler.BitmapFromWeb(p.FanartUrl);
                    return true;
                });

            if (platformInfo != null)
            {
                txt_Title.Text = platformInfo.Title;
                txt_company.Text = platformInfo.Developer;
                txt_description.Text = platformInfo.GetDescription();
                int grade;
                if (int.TryParse(platformInfo.Grade, out grade))
                    gradeUpDown.Value = grade;
            }
        }
    }
}