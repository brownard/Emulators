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
    internal partial class Wzd_NewEmu_Config : WzdPanel
    {
        Wzd_NewEmu_Main parent;
        public Wzd_NewEmu_Config(Wzd_NewEmu_Main parent)
        {
            InitializeComponent();
            this.parent = parent;
        }

        public override void UpdatePanel()
        {
            EmulatorProfile defaultProfile = parent.NewEmulator.DefaultProfile;
            pathTextBox.Text = defaultProfile.EmulatorPath;
            workingDirTextBox.Text = defaultProfile.WorkingDirectory;
            argumentsTextBox.Text = defaultProfile.Arguments;
            useQuotesCheckBox.Checked = defaultProfile.UseQuotes;
        }

        public override bool Next()
        {
            if (!string.IsNullOrEmpty(workingDirTextBox.Text) && !System.IO.Directory.Exists(workingDirTextBox.Text))
            {
                MessageBox.Show("Please enter a valid working directory, or leave empty to use the exe/bat directory.", "Invalid working directory", MessageBoxButtons.OK);
                return false;
            }
            EmulatorProfile defaultProfile = parent.NewEmulator.DefaultProfile;
            defaultProfile.WorkingDirectory = workingDirTextBox.Text;
            defaultProfile.Arguments = argumentsTextBox.Text;
            defaultProfile.UseQuotes = useQuotesCheckBox.Checked;
            return true;
        }
        
        private void button1_Click(object sender, EventArgs e)
        {
            EmulatorProfile defaultProfile = parent.NewEmulator.DefaultProfile;
            string initialDir;
            if (System.IO.Directory.Exists(workingDirTextBox.Text))
                initialDir = workingDirTextBox.Text;
            else if (defaultProfile.EmulatorPath.LastIndexOf("\\") > -1)
                initialDir = defaultProfile.EmulatorPath.Substring(0, defaultProfile.EmulatorPath.LastIndexOf("\\"));
            else
                initialDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            using (FolderBrowserDialog dlg = MP1Utils.OpenFolderDialog("Select working directory", initialDir))
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                    workingDirTextBox.Text = dlg.SelectedPath;
            }
        }

    }
}
