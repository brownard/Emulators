using Emulators.Database;
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
    internal partial class Conf_DBBackup : ContentPanel
    {
        public Conf_DBBackup()
        {
            InitializeComponent();

            Options options = EmulatorsCore.Options;
            options.EnterReadLock();
            backupPathTextBox.Text = options.BackupFile;
            backupThumbsCheckBox.Checked = options.BackupImages;

            restorePathTextBox.Text = options.RestoreFile;
            restoreThumbsCheckBox.Checked = options.RestoreImages;
            mergeRadioButton.Checked = options.RestoreMerge;

            BackupDropdownItem create = new BackupDropdownItem("Create new", MergeType.Create);
            BackupDropdownItem ignore = new BackupDropdownItem("Ignore", MergeType.Ignore);
            BackupDropdownItem merge = new BackupDropdownItem("Merge", MergeType.Merge);

            //emu merge settings
            emuMergeComboBox.DisplayMember = "DisplayMember";
            emuMergeComboBox.ValueMember = "ValueMember";
            emuMergeComboBox.Items.AddRange(new object[] { create, ignore, merge }); //allow all 3 options

            int index = sanitiseIndex(options.MergeEmulatorSetting, emuMergeComboBox.Items.Count);
            emuMergeComboBox.SelectedItem = emuMergeComboBox.Items[index];

            //games
            gameMergeComboBox.DisplayMember = "DisplayMember";
            gameMergeComboBox.ValueMember = "ValueMember";
            gameMergeComboBox.Items.AddRange(new object[] { ignore, merge }); //don't allow Create

            index = sanitiseIndex(options.MergeGameSetting, gameMergeComboBox.Items.Count);
            gameMergeComboBox.SelectedItem = gameMergeComboBox.Items[index];

            options.ExitReadLock();
            updateMergePanels();
        }

        //restore from specified xml
        private void restoreButton_Click(object sender, EventArgs e)
        {
            bool cleanRestore = cleanRadioButton.Checked;
            if (cleanRestore)
            {
                //warn user of db deletion
                DialogResult shouldClean = MessageBox.Show("All existing data in the database will be deleted.\r\nAre you sure you want to continue?", "Clean restore", MessageBoxButtons.YesNo);
                if (shouldClean != DialogResult.Yes)
                    return;
            }

            using (Conf_RestoreDlg dlg = new Conf_RestoreDlg(restorePathTextBox.Text, false))
            {
                dlg.CleanRestore = cleanRestore;
                dlg.EmulatorMergeType = ((BackupDropdownItem)emuMergeComboBox.SelectedItem).ValueMember;
                dlg.GameMergeType = ((BackupDropdownItem)gameMergeComboBox.SelectedItem).ValueMember;
                dlg.RestoreThumbs = restoreThumbsCheckBox.Checked;
                //display dialog (starts restore)
                if (dlg.ShowDialog() == DialogResult.OK)
                    MessageBox.Show("Restore completed successfully.");
            }
        }

        //update merge panel enabled status
        private void mergeRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            updateMergePanels();
        }

        void updateMergePanels()
        {
            if (mergeRadioButton.Checked)
                mergeGroupBox.Enabled = true;
            else
                mergeGroupBox.Enabled = false;
        }
        
        //display dialog and start db backup
        private void backupButton_Click(object sender, EventArgs e)
        {
            using (Conf_RestoreDlg dlg = new Conf_RestoreDlg(backupPathTextBox.Text, true))
            {
                dlg.BackupThumbs = backupThumbsCheckBox.Checked;
                if (dlg.ShowDialog() == DialogResult.OK)
                    MessageBox.Show("Backup completed successfully.");
            }
        }

        //show dialog allowing user to specify where to save backup
        private void backupPathButton_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog dlg = new SaveFileDialog())
            {
                dlg.Title = "Save file as...";
                dlg.Filter = "XML (*.xml)|*.xml";

                string initialDirectory = null;
                string path = backupPathTextBox.Text;

                if (!string.IsNullOrEmpty(path))
                {
                    int index = path.LastIndexOf("\\");
                    if (index > -1)
                        path = path.Remove(index);
                    if (System.IO.Directory.Exists(path))
                        initialDirectory = path;
                }
                if (initialDirectory == null)
                    initialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                dlg.RestoreDirectory = true;
                dlg.InitialDirectory = initialDirectory;

                if (dlg.ShowDialog() == DialogResult.OK)
                    backupPathTextBox.Text = dlg.FileName;
            }
        }

        //show dialog allowing user to select path to backup
        private void restorePathButton_Click(object sender, EventArgs e)
        {
            string title = "Select backup file...";
            string filter = "XML (*.xml)|*.xml";
            string initialDirectory = null;
            string path = restorePathTextBox.Text;

            if (!string.IsNullOrEmpty(path))
            {
                int index = path.LastIndexOf("\\");
                if (index > -1)
                    path = path.Remove(index);
                if (System.IO.Directory.Exists(path))
                    initialDirectory = path;
            }
            if (initialDirectory == null)
                initialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            using (OpenFileDialog dlg = MP1Utils.OpenFileDialog(title, filter, initialDirectory))
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                    restorePathTextBox.Text = dlg.FileName;
            }
        }

        int sanitiseIndex(int index, int itemCount)
        {
            if (index < 0)
                index = 0;
            else if (index >= itemCount)
                index = itemCount - 1;
           
            return index;
        }

        public override void ClosePanel()
        {
            Options opts = EmulatorsCore.Options;
            opts.EnterWriteLock();
            opts.BackupFile = backupPathTextBox.Text;
            opts.BackupImages = backupThumbsCheckBox.Checked;

            opts.RestoreFile = restorePathTextBox.Text;
            opts.RestoreImages = restoreThumbsCheckBox.Checked;
            opts.RestoreMerge = mergeRadioButton.Checked;

            opts.MergeEmulatorSetting = emuMergeComboBox.SelectedIndex;
            opts.MergeGameSetting = gameMergeComboBox.SelectedIndex;
            opts.ExitWriteLock();
        }
    }

    class BackupDropdownItem
    {
        public BackupDropdownItem(string displayMember, MergeType mergeType)
        {
            DisplayMember = displayMember;
            ValueMember = mergeType;
        }
        public string DisplayMember { get; set; }
        public MergeType ValueMember { get; set; }
    }
}
