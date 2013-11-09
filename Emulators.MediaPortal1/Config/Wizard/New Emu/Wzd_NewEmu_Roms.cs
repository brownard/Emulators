using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using Emulators.MediaPortal1;

namespace Emulators
{
    internal partial class Wzd_NewEmu_Roms : WzdPanel
    {
        Wzd_NewEmu_Main parent;
        public Wzd_NewEmu_Roms(Wzd_NewEmu_Main parent)
        {
            InitializeComponent();
            this.parent = parent;
            txt_Filter.Text = "*.*";
        }

        public override void UpdatePanel()
        {
            if (!string.IsNullOrEmpty(parent.NewEmulator.Filter))
                txt_Filter.Text = parent.NewEmulator.Filter;
        }

        public override bool Next()
        {
            if (!Directory.Exists(romDirTextBox.Text))
            {
                MessageBox.Show("Please enter a valid rom directory.", "Invalid rom directory", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return false;
            }
            parent.NewEmulator.PathToRoms = romDirTextBox.Text;
            parent.NewEmulator.Filter = txt_Filter.Text;
            return true;
        }

        private void refreshButton_Click(object sender, EventArgs e)
        {
            if (!Directory.Exists(romDirTextBox.Text))
            {
                MessageBox.Show("Please enter a valid rom directory.", "Invalid rom directory", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            string[] filters = txt_Filter.Text.Split(";".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            if (filters.Length < 1)
            {
                MessageBox.Show("Please enter a valid filter or use *.* to catch all files.", "Invalid filter", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            List<string> foundPaths = new List<string>();

            foreach (string filter in filters)
            {
                try
                {
                    string[] files = Directory.GetFiles(romDirTextBox.Text, filter, SearchOption.AllDirectories);
                    foreach (string file in files)
                        if (!foundPaths.Contains(file))
                            foundPaths.Add(file);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            foundPaths.Sort();
            dataGridView1.Rows.Clear();
            foreach (string path in foundPaths)
                dataGridView1.Rows.Add(path);
        }

        private void romDirButton_Click(object sender, EventArgs e)
        {
            string title = "Select directory containg Roms";
            string initialDir;
            if (System.IO.Directory.Exists(romDirTextBox.Text))
                initialDir = romDirTextBox.Text;
            else
                initialDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            using (FolderBrowserDialog dlg = MP1Utils.OpenFolderDialog(title, initialDir))
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                    romDirTextBox.Text = dlg.SelectedPath;
            }
        }
    }
}
