using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Emulators.MediaPortal1;

namespace Emulators
{
    internal partial class Wzd_NewEmu_Start : WzdPanel
    {
        Wzd_NewEmu_Main parent;
        public Wzd_NewEmu_Start(Wzd_NewEmu_Main parent)
        {
            InitializeComponent();
            this.parent = parent;
        }

        private void pathBrowseButton_Click(object sender, EventArgs e)
        {
            string filter = "Executables (*.bat, *.exe, *.cmd) | *.bat;*.exe;*.cmd";
            string initialDirectory;
            int index = pathTextBox.Text.LastIndexOf("\\");

            if (index > -1)
                initialDirectory = pathTextBox.Text.Remove(index);
            else
                initialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

            using (OpenFileDialog dlg = MP1Utils.OpenFileDialog("Path to executable", filter, initialDirectory))
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                    pathTextBox.Text = dlg.FileName;
            }
        }

        public override bool Next()
        {
            if (!pathTextBox.Text.IsExecutable() || !System.IO.File.Exists(pathTextBox.Text))
            {
                MessageBox.Show("Please enter a valid path to an .exe or .bat file (without arguments).", "Invalid file", MessageBoxButtons.OK);
                return false;
            }

            parent.NewEmulator = Emulator.CreateNewEmulator();
            parent.NewEmulator.DefaultProfile.EmulatorPath = pathTextBox.Text;
            autoConfig();
            return true;
        }

        void autoConfig()
        {
            EmulatorProfile autoSettings = EmuSettingsAutoFill.Instance.CheckForSettings(pathTextBox.Text);
            if (autoSettings == null)
                return;
            
            if (autoSettings.HasSettings && MessageBox.Show(string.Format("Would you like to use the recommended settings for {0}?", autoSettings.Title), "Use recommended settings?", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                if (!string.IsNullOrEmpty(autoSettings.Filters) && Options.Instance.GetBoolOption("autoconfemu"))
                    parent.NewEmulator.Filter = autoSettings.Filters;

                if (!string.IsNullOrEmpty(autoSettings.Platform))
                {
                    parent.NewEmulator.Platform = autoSettings.Platform;
                    parent.NewEmulator.Title = autoSettings.Platform;
                    parent.NewEmulator.CaseAspect = EmuSettingsAutoFill.Instance.GetCaseAspect(autoSettings.Platform);
                }

                if (autoSettings.HasSettings)
                {
                    EmulatorProfile defaultProfile = parent.NewEmulator.DefaultProfile;
                    defaultProfile.Arguments = autoSettings.Arguments;
                    defaultProfile.UseQuotes = autoSettings.UseQuotes;
                    defaultProfile.SuspendMP = autoSettings.SuspendMP;
                    defaultProfile.WorkingDirectory = autoSettings.WorkingDirectory;
                    defaultProfile.MountImages = autoSettings.MountImages;
                    defaultProfile.EscapeToExit = autoSettings.EscapeToExit;
                }
            }
        }
    }
}
