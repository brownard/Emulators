using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using Emulators.Database;

namespace Emulators
{
    public partial class Conf_RestoreDlg : Form
    {
        DatabaseBackup dbBackup = null;
        Thread worker = null;
        string path;
        bool backup = false;

        MergeType emulatorMergeType = MergeType.Merge;
        public MergeType EmulatorMergeType
        {
            get { return emulatorMergeType; }
            set { emulatorMergeType = value; }
        }
        MergeType gameMergeType = MergeType.Merge;
        public MergeType GameMergeType
        {
            get { return gameMergeType; }
            set { gameMergeType = value; }
        }
        public bool CleanRestore { get; set; }

        bool backupThumbs = true;
        public bool BackupThumbs
        {
            get { return backupThumbs; }
            set { backupThumbs = value; }
        }

        bool restoreThumbs = true;
        public bool RestoreThumbs
        {
            get { return restoreThumbs; }
            set { restoreThumbs = value; }
        }

        internal Conf_RestoreDlg(string path, bool backup)
        {
            InitializeComponent();
            this.backup = backup;
            this.Text = backup ? "Backup" : "Restore";
            this.path = path;
            this.FormClosing += new FormClosingEventHandler(Conf_RestoreDlg_FormClosing);
        }

        void Conf_RestoreDlg_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (worker != null && worker.IsAlive)
                e.Cancel = true;
        }

        private void Conf_RestoreDlg_Load(object sender, EventArgs e)
        {
            dbBackup = new DatabaseBackup();
            dbBackup.BackupProgress += dbBackup_OnBackupProgress;
            dbBackup.BackupError += dbBackup_OnBackupDataError;
            dbBackup.Completed += dbBackup_Completed;
            if (backup)
            {
                dbBackup.BackupThumbs = backupThumbs;
            }
            else
            {
                dbBackup.RestoreThumbs = restoreThumbs;
                dbBackup.EmulatorMergeType = emulatorMergeType;
                dbBackup.GameMergeType = gameMergeType;
                dbBackup.CleanRestore = CleanRestore;
            }

            worker = new Thread(new ThreadStart(delegate()
                {
                    if (backup)
                        dbBackup.Backup(path);
                    else
                        dbBackup.Restore(path);
                }));
            worker.Start();
        }

        void dbBackup_Completed(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(delegate() { dbBackup_Completed(sender, e); }));
                return;
            }
            label1.Text = "Complete";
            DialogResult = System.Windows.Forms.DialogResult.OK;
            closeForm();
        }

        void dbBackup_OnBackupDataError(object sender, BackupErrorEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(delegate() { dbBackup_OnBackupDataError(sender, e); }));
                return;
            }
            MessageBox.Show(string.Format("Error: {0}", e.Message), "Error", MessageBoxButtons.OK);
            closeForm();
        }

        void dbBackup_OnBackupProgress(object sender, BackupProgressEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(delegate() { dbBackup_OnBackupProgress(sender, e); }));
                return;
            }

            progressBar1.Value = e.Percent;
            label1.Text = string.Format("{0} / {1} - {2}", e.Current, e.Total, e.Message);
        }

        void closeForm()
        {
            if (worker != null && worker.IsAlive)
                worker.Join();
            Close();
        }
    }

    enum RestoreType
    {
        Backup,
        Restore
    }
}
