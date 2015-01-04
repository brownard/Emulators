using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Emulators.MediaPortal1;
using Emulators.AutoConfig;
using System.IO;

namespace Emulators
{
    internal partial class Wzd_NewEmu_Start : WzdPanel
    {
        const string FILE_FILTER = "Executables (*.bat, *.exe, *.cmd) | *.bat;*.exe;*.cmd";
        Wzd_NewEmu_Main parent;

        public Wzd_NewEmu_Start(Wzd_NewEmu_Main parent)
        {
            InitializeComponent();
            this.parent = parent;
        }

        private void pathBrowseButton_Click(object sender, EventArgs e)
        {
            string path = pathTextBox.Text;
            int index = path.LastIndexOf("\\");
            string initialDirectory;
            if (index > -1)
                initialDirectory = path.Remove(index);
            else
                initialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

            using (OpenFileDialog dlg = MP1Utils.OpenFileDialog("Path to executable", FILE_FILTER, initialDirectory))
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                    pathTextBox.Text = dlg.FileName;
            }
        }

        public override bool Next()
        {
            string path = pathTextBox.Text;
            if (!path.IsExecutable() || !File.Exists(path))
            {
                MessageBox.Show("Please enter a valid path to an .exe or .bat file (without arguments).", "Invalid file", MessageBoxButtons.OK);
                return false;
            }

            parent.NewEmulator = Emulator.CreateNewEmulator();
            parent.NewEmulator.DefaultProfile.EmulatorPath = path;
            autoConfigureEmulator(path);
            return true;
        }

        void autoConfigureEmulator(string path)
        {
            EmulatorConfig autoConfig = EmuAutoConfig.Instance.CheckForSettings(pathTextBox.Text);
            if (autoConfig == null)
                return;

            if (confirmUseAutoConfig(autoConfig.Name))
            {
                if (!string.IsNullOrEmpty(autoConfig.Filters))
                    parent.NewEmulator.Filter = autoConfig.Filters;

                if (!string.IsNullOrEmpty(autoConfig.Platform))
                {
                    parent.NewEmulator.Platform = autoConfig.Platform;
                    parent.NewEmulator.Title = autoConfig.Platform;
                    parent.NewEmulator.CaseAspect = autoConfig.CaseAspect;
                }

                ProfileConfig profile = autoConfig.ProfileConfig;
                if (profile != null)
                {
                    EmulatorProfile defaultProfile = parent.NewEmulator.DefaultProfile;
                    defaultProfile.Arguments = profile.Arguments;
                    if (profile.UseQuotes.HasValue)
                        defaultProfile.UseQuotes = profile.UseQuotes.Value;
                    if (profile.SuspendMP.HasValue)
                        defaultProfile.SuspendMP = profile.SuspendMP.Value;
                    if (profile.MountImages.HasValue)
                        defaultProfile.MountImages = profile.MountImages.Value;
                    if (profile.EscapeToExit.HasValue)
                        defaultProfile.EscapeToExit = profile.EscapeToExit.Value;

                    if (profile.WorkingDirectory == EmuAutoConfig.USE_EMULATOR_DIRECTORY)
                    {
                        try
                        {
                            FileInfo file = new FileInfo(path);
                            if (file.Exists)
                                defaultProfile.WorkingDirectory = file.Directory.FullName;
                        }
                        catch { }
                    }
                    else
                    {
                        defaultProfile.WorkingDirectory = profile.WorkingDirectory;
                    }
                }
            }
        }

        bool confirmUseAutoConfig(string settingsName)
        {
            string message = string.Format("Would you like to use the recommended settings for {0}?", settingsName);
            return MessageBox.Show(message, "Use recommended settings?", MessageBoxButtons.YesNo) == DialogResult.Yes;
        }
    }
}
