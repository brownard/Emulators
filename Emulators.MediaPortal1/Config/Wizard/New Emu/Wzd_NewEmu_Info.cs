using Emulators.AutoConfig;
using Emulators.MediaPortal1;
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
        public Wzd_NewEmu_Info(Wzd_NewEmu_Main parent)
        {
            InitializeComponent();
            platformComboBox.DisplayMember = "Text";
            platformComboBox.DataSource = Dropdowns.GetSystems();
            txt_Title.Text = "New Emulator";
            this.parent = parent;
        }

        bool firstLoad = true;
        public override void UpdatePanel()
        {
            if (!string.IsNullOrEmpty(parent.NewEmulator.Title))
                txt_Title.Text = parent.NewEmulator.Title;

            int index = platformComboBox.FindStringExact(parent.NewEmulator.Platform);
            if (index > -1)
                platformComboBox.SelectedIndex = index;

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
            EmulatorInfo lEmuInfo = new EmulatorScraperHandler().UpdateEmuInfo(platformComboBox.Text, (o) =>
                {
                    EmulatorInfo emuInfo = (EmulatorInfo)o;
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
                    parent.Logo = ImageHandler.BitmapFromWeb(emuInfo.LogoUrl);
                    parent.Fanart = ImageHandler.BitmapFromWeb(emuInfo.FanartUrl);
                    return true;
                });                      

            if (lEmuInfo != null)
            {
                txt_Title.Text = lEmuInfo.Title;
                txt_company.Text = lEmuInfo.Developer;
                txt_description.Text = lEmuInfo.GetDescription();
                int grade;
                if (int.TryParse(lEmuInfo.Grade, out grade))
                    gradeUpDown.Value = grade;
                return;
            }
        }
    }
}
