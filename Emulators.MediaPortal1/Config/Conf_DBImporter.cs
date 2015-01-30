using Emulators.Database;
using Emulators.Import;
using Emulators.MediaPortal1;
using Emulators.MediaPortal1.Properties;
using Emulators.Scrapers;
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
    internal partial class Conf_DBImporter : ContentPanel
    {
        const string NO_FILES_TO_IMPORT = "No new files to import";

        Image[] statusImages;
        Image[] mergeImages;
        BindingSource importerBindingSource = null;
        List<RomMatch> importBindingList = null;
        
        Importer importer;
        public Importer Importer 
        {
            get { return importer; }
            set
            {
                importer = value;
                if (importer != null)
                {
                    importer.ProgressChanged += importer_Progress;
                    importer.ImportStatusChanged += importer_ImportStatusChanged;
                    importer.RomStatusChanged += importer_RomStatusChanged;
                }
            }
        }

        public Conf_DBImporter()
        {
            InitializeComponent();
            
            importGridView.SelectionChanged += new EventHandler(importGridView_SelectionChanged);
            progressLabel.Text = "";

            importBindingList = new List<RomMatch>();
            importerBindingSource = new BindingSource();
            importerBindingSource.AllowNew = true;
            importerBindingSource.DataSource = importBindingList;
            importerBindingSource.ListChanged += new ListChangedEventHandler(importerBindingSource_ListChanged);

            //setup import grid
            importGridView.AutoGenerateColumns = false;
            importGridView.DataSource = importerBindingSource;
            importGridView.EditingControlShowing += new DataGridViewEditingControlShowingEventHandler(importGridView_EditingControlShowing);
            importGridView.CellEndEdit += new DataGridViewCellEventHandler(importGridView_CellEndEdit);
            importGridView.ColumnHeaderMouseClick += new DataGridViewCellMouseEventHandler(importGridView_ColumnHeaderMouseClick);
            importGridView.RowsAdded += importGridView_RowsAdded;
            comboBoxChangedHandler = new EventHandler(resultsComboBox_SelectedIndexChanged);

            statusImages = new[] { Resources.information, Resources.approved, Resources.accept };
            mergeImages = new[] { Resources.arrow_divide, Resources.arrow_join };
        }  

        void importGridView_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            for(int x = e.RowIndex; x < e.RowIndex + e.RowCount; x++)            
                refreshRow(x);
        }

        private void Conf_DBImporter_Load(object sender, EventArgs e)
        {
            
        }

        #region GridView Events

        void importGridView_SelectionChanged(object sender, EventArgs e)
        {
            updateButtons();
        }

        void importGridView_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex < 0 || e.ColumnIndex >= importGridView.Columns.Count)
                return;
            DataGridViewColumn column = importGridView.Columns[e.ColumnIndex];
            SortOrder sortOrder = getNextSortDirection(column.HeaderCell.SortGlyphDirection);
            int direction = sortOrder == SortOrder.Descending ? -1 : 1;
            if (sortOrder == SortOrder.None)
                importBindingList.Sort((RomMatch a, RomMatch b) => { return a.BindingSourceIndex.CompareTo(b.BindingSourceIndex); });
            else if (column == columnFilename)
                importBindingList.Sort((RomMatch a, RomMatch b) => { return a.Path.CompareTo(b.Path) * direction; });
            else if (column == columnIcon)
                importBindingList.Sort((RomMatch a, RomMatch b) => 
                {
                    int compare = ((int)a.Status).CompareTo((int)b.Status) * direction;
                    if(compare == 0)
                        compare = a.BindingSourceIndex.CompareTo(b.BindingSourceIndex);
                    return compare;
                });
            else
                return;

            importerBindingSource.ResetBindings(false);
            column.HeaderCell.SortGlyphDirection = sortOrder;
        }

        SortOrder getNextSortDirection(SortOrder sortOrder)
        {
            if (sortOrder == SortOrder.None)
                return SortOrder.Ascending;
            if (sortOrder == SortOrder.Ascending)
                return SortOrder.Descending;

            return SortOrder.None;
        }

        void importGridView_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (importGridView.CurrentCell == null || importGridView.CurrentCell.ColumnIndex != columnTitle.Index)
                return;
            
            currentComboBox = e.Control as ComboBox;
            if (currentComboBox != null)
            {
                currentComboBox.SelectedIndexChanged += comboBoxChangedHandler;
                currentComboBox.Tag = importGridView.Rows[importGridView.CurrentCell.RowIndex].DataBoundItem;
                int maxWidth = MP1Utils.GetComboBoxDropDownWidth(currentComboBox);
                columnTitle.DropDownWidth = maxWidth > columnTitle.Width ? maxWidth : columnTitle.Width;
            }
        }

        EventHandler comboBoxChangedHandler;
        ComboBox currentComboBox = null;
        void importGridView_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == columnTitle.Index && currentComboBox != null)
                currentComboBox.SelectedIndexChanged -= comboBoxChangedHandler;
        }

        void resultsComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox resultsComboBox = (ComboBox)sender;
            RomMatch romMatch = resultsComboBox.Tag as RomMatch;
            ScraperResult newResult = resultsComboBox.SelectedItem as ScraperResult;
            if (newResult != null && newResult != importGridView.CurrentCell.Value)
                importer.UpdateSelectedMatch(romMatch, newResult);
            importGridView.EndEdit();
        }

        void importGridView_CellMouseEnter(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex < 0 || e.RowIndex < 0)
                return;
            if (e.ColumnIndex == columnFilename.Index)
            {
                RomMatch romMatch = (RomMatch)importerBindingSource[e.RowIndex];
                importGridView.Rows[e.RowIndex].Cells[e.ColumnIndex].ToolTipText = romMatch.ToolTip;
            }
        }

        void importerBindingSource_ListChanged(object sender, ListChangedEventArgs e)
        {
            if (e.ListChangedType == ListChangedType.Reset && importGridView.Rows.Count > 0)
            {
                for (int x = 0; x < importGridView.Rows.Count; x++)
                    refreshRow(x);
            }
        }

        #endregion

        #region Importer Events

        void importer_Progress(object sender, ImportProgressEventArgs e)
        {
            if (closing)
                return;
            if (InvokeRequired)
            {
                try
                {
                    //Make sure we only execute on main thread
                    BeginInvoke(new MethodInvoker(delegate()
                    {
                        importer_Progress(sender, e);
                    }
                        ));
                }
                catch (ObjectDisposedException) { } //form closed
                return;
            }

            if (closing)
                return;

            progressBar.Value = e.Percent;
            if (e.Total > 0)
                progressLabel.Text = string.Format("{0}/{1} - {2}", e.Current, e.Total, e.Description);
            else
                progressLabel.Text = e.Description;

            progressBar.Visible = true;
            progressLabel.Visible = true;
        }

        void importer_ImportStatusChanged(object sender, ImportStatusEventArgs e)
        {
            if (closing)
                return;
            if (InvokeRequired)
            {
                try
                {
                    //Make sure we only execute on main thread
                    BeginInvoke(new MethodInvoker(delegate()
                    {
                        importer_ImportStatusChanged(sender, e);
                    }
                    ));
                }
                catch (ObjectDisposedException) { } //form closed
                return;
            }

            if (closing)
                return;
            if (e.Action == ImportAction.ImportStarting)
            {
                importerBindingSource.Clear();
                restarting = false;
                return;
            }
            else if (restarting)
                return;

            switch (e.Action)
            {
                case ImportAction.NoFilesFound:
                    progressBar.Visible = false;
                    progressLabel.Visible = true;
                    progressLabel.Text = NO_FILES_TO_IMPORT;
                    break;
                //occurs after DB refresh on importer load, add all files to grid
                case ImportAction.PendingFilesAdded:
                    addRow(e.NewItems);
                    break;
            }
        }

        void importer_RomStatusChanged(object sender, RomStatusEventArgs e)
        {
            if (closing || restarting)
                return;
            if (InvokeRequired)
            {
                try
                {
                    //Make sure we only execute on main thread
                    BeginInvoke(new MethodInvoker(delegate()
                    {
                        importer_RomStatusChanged(sender, e);
                    }
                    ));
                }
                catch (ObjectDisposedException) { } //form closed
                return;
            }

            if (closing || restarting)
                return;

            RomMatch romMatch;
            if (e.Status == RomMatchStatus.PendingHash)
            {
                romMatch = e.RomMatch;
                if (!checkRow(romMatch)) //if false, add a new row
                {
                    addRow(romMatch);
                    return;
                }
            }
            else if (e.Status == RomMatchStatus.Ignored || e.Status == RomMatchStatus.Removed)
            {
                importerBindingSource.Remove(e.RomMatch);
                return;
            }

            int rowNum = importerBindingSource.IndexOf(e.RomMatch);
            if (rowNum < 0) //match not in grid
                return;

            romMatch = e.RomMatch;
            if (romMatch.Status == RomMatchStatus.Ignored && e.Status != RomMatchStatus.PendingHash && e.Status != RomMatchStatus.Ignored)
                return;

            refreshRow(rowNum);
            updateButtons();
        }

        #endregion

        #region DataSource Methods

        //adds a List of RomMatches to the BindingSource/Grid
        void addRow(List<RomMatch> romMatches)
        {
            if (romMatches == null)
                return;

            for (int x = 0; x < romMatches.Count; x++)
                addRow(romMatches[x]);
        }

        void addRow(RomMatch romMatch)
        {
            int rowNum = importerBindingSource.Add(romMatch);
            romMatch.BindingSourceIndex = rowNum;
            if (rowNum == 0 && importGridView.Rows.Count > 0)
                importGridView.Rows[0].Selected = true; //if first item select it
        }

        //checks if a RomMatch with the same ID is already in the BindingSource, if so it's status is reset else it's added
        bool checkRow(RomMatch romMatch)
        {
            if (importerBindingSource.Contains(romMatch))
                return true;

            foreach (DataGridViewRow row in importGridView.Rows)
            {
                //Game already in gridview, must be previously ignored. Reset status and return
                RomMatch rowRomMatch = (RomMatch)row.DataBoundItem;
                if (romMatch.ID == rowRomMatch.ID)
                {
                    int rowNum = row.Index;
                    importerBindingSource.RemoveAt(rowNum);
                    romMatch.BindingSourceIndex = rowRomMatch.BindingSourceIndex;
                    importerBindingSource.Insert(rowNum, romMatch);
                    return true;
                }
            }
            return false;
        }

        void refreshRow(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= importGridView.Rows.Count)
                return;

            Image statusIcon = new Bitmap(1, 1);
            string statusTxt = "";
            RomMatch romMatch = importGridView.Rows[rowIndex].DataBoundItem as RomMatch;
            if (romMatch == null)
                return;

            switch (romMatch.Status)
            {
                case RomMatchStatus.PendingHash: //reset match status
                    statusTxt = "";
                    statusIcon = statusImages[0]; //Emulators.MediaPortal1.Properties.Resources.information;
                    break;
                case RomMatchStatus.NeedsInput: //user input requested
                    statusTxt = "Needs input";
                    statusIcon = statusImages[0]; //Emulators.MediaPortal1.Properties.Resources.information;
                    break;
                case RomMatchStatus.Approved: //match user/auto approved
                    statusTxt = "Approved";
                    statusIcon = statusImages[1]; //Emulators.MediaPortal1.Properties.Resources.approved;
                    break;
                case RomMatchStatus.Committed: //Game updated and commited
                    statusTxt = "Commited";
                    statusIcon = statusImages[2]; //Emulators.MediaPortal1.Properties.Resources.accept;
                    break;
                default:
                    return;
            }

            //set status icon
            importGridView.Rows[rowIndex].Cells[columnIcon.Name].Value = statusIcon;
            importGridView.Rows[rowIndex].Cells[columnIcon.Name].ToolTipText = statusTxt;

            //setup ComboBox cell with possible matches
            DataGridViewComboBoxCell comboBox = (DataGridViewComboBoxCell)importGridView.Rows[rowIndex].Cells[columnTitle.Name]; //get the ComboBox cell
            comboBox.DisplayMember = "DisplayMember";
            comboBox.ValueMember = "Self";
            comboBox.Value = null;
            comboBox.Items.Clear(); //remove any leftovers

            bool check = romMatch.GameDetails != null;

            foreach (ScraperResult details in romMatch.PossibleGameDetails)
            {
                int index = comboBox.Items.Add(details); //possible matches

                if (check && romMatch.GameDetails == details)
                    comboBox.Value = comboBox.Items[index];
            }

            if (!check && comboBox.Items.Count > 0)
                comboBox.Value = comboBox.Items[0]; //select first value
        }

        #endregion

        #region Panel Overrides

        public override void UpdatePanel()
        {
            if (importer.ImporterStatus == ImportAction.ImportStopped)
                importer.Restart();
            base.UpdatePanel();
        }

        volatile bool closing = false;
        public override void ClosePanel()
        {
            closing = true;
            if (importer != null)
            {
                importer.ProgressChanged -= importer_Progress;
                importer.ImportStatusChanged -= importer_ImportStatusChanged;
                importer.RomStatusChanged -= importer_RomStatusChanged;
                new System.Threading.Thread(importer.Stop).Start();
            }
            base.ClosePanel();
        }

        #endregion

        #region Buttons

        void updateButtons()
        {
            if (importGridView.SelectedRows.Count == 0)
                return;
            else if (importGridView.SelectedRows.Count > 1)
            {
                approveButton.Enabled = true;
                findButton.Enabled = false;
                mergeButton.Image = mergeImages[1];
                importToolTip.SetToolTip(mergeButton, "Merge into multi-disc game");
                mergeButton.Enabled = true;
                return;
            }
            findButton.Enabled = true;

            RomMatch match = importGridView.SelectedRows[0].DataBoundItem as RomMatch;
            RomMatchStatus status = match.Status;

            if (match.IsMultiFile)
            {
                mergeButton.Image = mergeImages[0];
                importToolTip.SetToolTip(mergeButton, "Split multi-disc game");
                mergeButton.Enabled = true;
            }
            else
            {
                mergeButton.Image = mergeImages[1];
                importToolTip.SetToolTip(mergeButton, "Merge into multi-disc game");
                mergeButton.Enabled = false;
            }

            //Allow approve only if match has been checked and not ignored
            if ((status == RomMatchStatus.Approved || status == RomMatchStatus.NeedsInput) && importGridView.SelectedRows[0].Cells[columnTitle.Name].Value != null)
            {
                approveButton.Enabled = true;
            }
            else
            {
                approveButton.Enabled = false;
            }
        }

        bool restarting = false;
        private void refreshButton_Click(object sender, EventArgs e)
        {
            if (!restarting && importer != null)
            {
                restarting = true;
                progressBar.Visible = false;
                progressLabel.Visible = true;
                progressLabel.Text = "Restarting...";
                importer.Restart();
            }
        }

        //Approve selected match
        void approveButton_Click(object sender, EventArgs e)
        {
            importGridView.EndEdit();
            if (importGridView.SelectedRows.Count < 1)
                return;

            List<RomMatch> matches = new List<RomMatch>();

            foreach (DataGridViewRow row in importGridView.SelectedRows)
            {
                RomMatch romMatch = row.DataBoundItem as RomMatch;
                if (romMatch == null)
                    continue;

                matches.Add(romMatch);
            }

            if (matches.Count == 0)
                return;
            else if (matches.Count == 1)
                importer.Approve(matches[0]);
            else
            {
                BackgroundTaskHandler handler = new BackgroundTaskHandler();
                handler.StatusDelegate = () => { return "Approving roms..."; };
                handler.ActionDelegate = () =>
                {
                    importer.Approve(matches);
                };
                using (Conf_ProgressDialog dlg = new Conf_ProgressDialog(handler))
                    dlg.ShowDialog();
            }

            updateButtons();
        }

        //Manual search
        private void findButton_Click(object sender, EventArgs e)
        {
            if (importGridView.SelectedRows.Count == 0)
                return;

            RomMatch match = importGridView.SelectedRows[0].DataBoundItem as RomMatch;
            Conf_ManualSearch searchDlg = new Conf_ManualSearch(match.SearchTitle);
            if (searchDlg.ShowDialog() == DialogResult.OK)
            {
                match.SearchTitle = searchDlg.SearchTerm;
                importer.ReProcess(match);
            }
            updateButtons();
        }

        void ignoreButton_Click(object sender, EventArgs e)
        {
            if (importGridView.SelectedRows.Count < 1)
                return;

            List<RomMatch> matches = new List<RomMatch>();
            foreach (DataGridViewRow row in importGridView.SelectedRows)
            {
                RomMatch match = row.DataBoundItem as RomMatch;
                if (match != null)
                    matches.Add(match);
            }

            if (matches.Count == 0)
                return;
            else if (matches.Count == 1)
                importer.Ignore(matches[0]);
            else
            {
                BackgroundTaskHandler handler = new BackgroundTaskHandler();
                handler.StatusDelegate = () => { return "Ignoring roms..."; };
                handler.ActionDelegate = () =>
                {
                    importer.Ignore(matches);
                };
                using (Conf_ProgressDialog dlg = new Conf_ProgressDialog(handler))
                    dlg.ShowDialog();
            }

            updateButtons();
        }

        void delRomButton_Click(object sender, EventArgs e)
        {
            if (importGridView.SelectedRows.Count < 1)
                return;

            if (MessageBox.Show(
                "Are you sure you want to delete the selected Game(s) and add them to the ignored files list?",
                "Delete Game(s)?",
                MessageBoxButtons.YesNo) != DialogResult.Yes)
                return;

            List<Game> games = new List<Game>();
            foreach (DataGridViewRow row in importGridView.SelectedRows)
            {
                RomMatch match = row.DataBoundItem as RomMatch;
                if (match != null && match.Game != null)
                    games.Add(match.Game);
            }

            BackgroundTaskHandler<Game> handler = new BackgroundTaskHandler<Game>() { Items = games };
            handler.StatusDelegate = game => { return "removing " + game.Title; };
            handler.ActionDelegate = game =>
            {                
                foreach (GameDisc disc in game.Discs)
                    EmulatorsCore.Options.AddIgnoreFile(disc.Path);                    
                game.Delete();
            };

            using (Conf_ProgressDialog progressDlg = new Conf_ProgressDialog(handler))
                progressDlg.ShowDialog();
        }

        void mergeButton_Click(object sender, EventArgs e)
        {
            importGridView.EndEdit();
            if (importGridView.SelectedRows.Count > 1)
                mergeSelectedRows();
            else if (importGridView.SelectedRows.Count == 1)
                unMergeSelectedRow();
            else
                return;
            updateButtons();
        }

        void mergeSelectedRows()
        {
            if (importGridView.SelectedRows.Count < 2)
                return;

            RomMatch romMatch = importGridView.SelectedRows[importGridView.SelectedRows.Count - 1].DataBoundItem as RomMatch;
            if (romMatch == null)
                return;
            Game game = romMatch.Game;
            if (game == null)
                return;

            List<Game> removeGames = new List<Game>();
            for (int x = importGridView.SelectedRows.Count - 2; x > -1; x--)
            {
                RomMatch match = importGridView.SelectedRows[x].DataBoundItem as RomMatch;
                if (match == null || match.Game == null || match.Game.Discs.Count > 1)
                    continue;

                GameDisc disc = match.Game.CurrentDisc;
                GameDisc newDisc = new GameDisc(disc.Path, disc.LaunchFile) { Number = game.Discs.Count + 1 };
                game.Discs.Add(newDisc);
                removeGames.Add(match.Game);
            }
            lock (EmulatorsCore.Database.SyncRoot)
            {
                EmulatorsCore.Database.ExecuteTransaction(removeGames, removeGame =>
                {
                    importer.Remove(removeGame.Id);
                    removeGame.Delete();
                });
            }

            romMatch.ResetDisplayInfo();
        }

        void unMergeSelectedRow()
        {
            if (importGridView.SelectedRows.Count != 1)
                return;

            DataGridViewRow selectedRow = importGridView.SelectedRows[0];
            RomMatch romMatch = selectedRow.DataBoundItem as RomMatch;
            if (romMatch == null)
                return;
            List<GameDisc> discs = romMatch.Game.Discs;
            if (discs.Count < 2)
                return;
            romMatch.ResetDisplayInfo();
            romMatch.Game.CurrentDisc = null;
            romMatch.Game.SaveGamePlayInfo();
            List<Game> newGames = new List<Game>();
            for (int x = 1; x < discs.Count; x++)
            {
                GameDisc disc = discs[x];
                Game newGame = new Game(romMatch.Game.ParentEmulator, disc.Path, disc.LaunchFile);
                newGame.Commit();
                newGames.Add(newGame);
                importerBindingSource.Insert(selectedRow.Index + x, new RomMatch(newGame) { BindingSourceIndex = romMatch.BindingSourceIndex + x });
            }
            importer.AddGames(newGames);
        }

        #endregion
    }
}
