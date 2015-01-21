﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using Emulators.Import;
using Emulators.MediaPortal1;
using Emulators.Database;

namespace Emulators
{
    internal partial class Conf_DBBrowser : ContentPanel
    {
        List<ListViewItem> dbGames;        
        Game selectedGame = null;
        bool saveSelectedGame = false;
        bool saveThumbs = false;
        bool saveDiscs = false;
        bool savePCSettings = false;
        bool thumbsLoaded = false;
        bool allowChangedEvents = true;
        ListViewItem selectedListItem = null;
        ThumbGroup itemThumbs = null;
        BindingSource discBindingSource = null;

        public Importer Importer { get; set; }  

        public Conf_DBBrowser()
        {
            InitializeComponent();
            addEventHandlers();
            setupToolTip();
            this.HandleDestroyed += new EventHandler(Conf_DBBrowser_HandleDestroyed);
            emuComboBox.SelectedIndexChanged += new EventHandler(emuComboBox_SelectedIndexChanged);

            discBindingSource = new BindingSource();
            discBindingSource.AllowNew = true;
            discBindingSource.DataSource = typeof(GameDisc);

            discGridView.CellContentClick += new DataGridViewCellEventHandler(discGridView_CellContentClick);
            discGridView.VisibleChanged += new EventHandler(discGridView_VisibleChanged);          
            discGridView.AutoGenerateColumns = false;
            discGridView.DataSource = discBindingSource;

            tabControl1.SelectedIndexChanged += new EventHandler(tabControl1_TabIndexChanged);
            pcProfileComboBox.SelectedIndexChanged += new EventHandler(pcProfileComboBox_SelectedIndexChanged);
            tabControl1.TabPages.Remove(pcSettingsTab);
        }

        void tabControl1_TabIndexChanged(object sender, EventArgs e)
        {
            if (!thumbsLoaded && tabControl1.SelectedTab == thumbsTab)
            {
                thumbsLoaded = true;
                setGameArt(itemThumbs);
            }
        }

        bool firstGridLoad = true;
        void discGridView_VisibleChanged(object sender, EventArgs e)
        {
            if (firstGridLoad)
            {
                firstGridLoad = false;
                discGridView.Refresh();
            }
        }

        void clearPanel()
        {
            allowChangedEvents = false;
            idLabel.Text = "";
            txt_Title.Text = "";
            txt_company.Text = "";
            txt_description.Text = "";
            txt_yearmade.Text = "";
            txt_genre.Text = "";
            gradeUpDown.Value = 0;
            txt_Manual.Text = "";
            chk_Favourite.Checked = false;
            videoTextBox.Text = "";
            playCountLabel.Text = "";
            lastPlayLabel.Text = "";
            profileComboBox.Items.Clear();
            setGameArt(null);
            if (itemThumbs != null)
            {
                itemThumbs.Dispose();
                itemThumbs = null;
            }
            discBindingSource.Clear();
            allowChangedEvents = true;
        }

        void setGameArt(ThumbGroup thumbGroup)
        {
            bool lAllowEvents = allowChangedEvents;
            allowChangedEvents = false;
            pnlBoxFront.ThumbGroup = thumbGroup;
            pnlBoxBack.ThumbGroup = thumbGroup;
            pnlTitleScreen.ThumbGroup = thumbGroup;
            pnlInGameScreen.ThumbGroup = thumbGroup;
            pnlFanart.ThumbGroup = thumbGroup;
            allowChangedEvents = lAllowEvents;
        }

        #region Event Handlers

        void Conf_DBBrowser_HandleDestroyed(object sender, EventArgs e)
        {
            ClosePanel();
        }

        void onItemChanged(object sender, EventArgs e)
        {
            if (allowChangedEvents)
                saveSelectedGame = true;
        }

        void onThumbChanged(object sender, EventArgs e)
        {
            if (allowChangedEvents)
                saveThumbs = true;
        }

        void onPCSettingsChanged(object sender, EventArgs e)
        {
            if (allowChangedEvents)
                savePCSettings = true;
        }

        void addEventHandlers()
        {
            dBListView.ItemSelectionChanged += new ListViewItemSelectionChangedEventHandler(dBListView_ItemSelectionChanged);
            txt_Title.TextChanged += new EventHandler(onItemChanged);
            txt_company.TextChanged += new EventHandler(onItemChanged);
            txt_description.TextChanged += new EventHandler(onItemChanged);
            txt_genre.TextChanged += new EventHandler(onItemChanged);
            txt_yearmade.TextChanged += new EventHandler(onItemChanged);
            txt_Manual.TextChanged += new EventHandler(onItemChanged);
            gradeUpDown.ValueChanged += new EventHandler(onItemChanged);
            chk_Favourite.CheckedChanged += new EventHandler(onItemChanged);
            videoTextBox.TextChanged += new EventHandler(onItemChanged);
            profileComboBox.SelectedIndexChanged += new EventHandler(onItemChanged);

            pnlBoxFront.BackgroundImageChanged += new EventHandler(onThumbChanged);
            pnlBoxBack.BackgroundImageChanged += new EventHandler(onThumbChanged);
            pnlTitleScreen.BackgroundImageChanged += new EventHandler(onThumbChanged);
            pnlInGameScreen.BackgroundImageChanged += new EventHandler(onThumbChanged);
            pnlFanart.BackgroundImageChanged += new EventHandler(onThumbChanged);

            suspendMPCheckBox.CheckedChanged += new EventHandler(onPCSettingsChanged);
            argumentsTextBox.TextChanged += new EventHandler(onPCSettingsChanged);
            launchedFileTextBox.TextChanged += new EventHandler(onPCSettingsChanged);
            preCommandText.TextChanged += new EventHandler(onPCSettingsChanged);
            preCommandWaitCheck.CheckedChanged += new EventHandler(onPCSettingsChanged);
            preCommandWindowCheck.CheckedChanged += new EventHandler(onPCSettingsChanged);
            postCommandText.TextChanged += new EventHandler(onPCSettingsChanged);
            postCommandWaitCheck.CheckedChanged += new EventHandler(onPCSettingsChanged);
            postCommandWindowCheck.CheckedChanged += new EventHandler(onPCSettingsChanged);
        }
        
        #endregion
        
        #region Database ListView

        volatile bool listLoading = false;
        void initListView()
        {
            if (listLoading)
                return;

            listLoading = true;

            dbGames = new List<ListViewItem>();

            loadingRomsPanel.Visible = true;
            dBListView.BeginUpdate();
            dBListView.Items.Clear();
            dBListView.EndUpdate();

            Thread thread = new Thread(new ThreadStart(delegate()
            {
                List<ListViewItem> newItems = new List<ListViewItem>();
                foreach (Game game in Game.GetAll(true))
                    newItems.Add(new ListViewItem(game.Title) { Tag = game });
                try
                {
                    BeginInvoke(new MethodInvoker(delegate
                    {
                        dbGames = newItems;
                        updateEmuBox(); //emu dropdown will handle list population
                        loadingRomsPanel.Visible = false;
                        listLoading = false;
                    }));
                }
                catch { listLoading = false; }
            }));

            thread.Name = "Rom browser populator";
            thread.Start();
        }

        void dBListView_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            //if no items are selected, or we have just started selecting multiple items, clear the panel.
            //With multiple selections, only call the below code on the 2nd selection otherwise it will be
            //called for every new item added - which is unnecessary and expensive. 
            if (dBListView.SelectedItems.Count == 2)
            {
                selectedGame = null;
                selectedListItem = null;
                if (itemThumbs != null)
                {
                    itemThumbs.Dispose();
                    itemThumbs = null;
                }
                clearPanel();
                return;
            }

            //When the user changes selection in the list view the SelectionChanged event is fired twice,
            //once for the item losing selection and once for the item gaining it, ensure we only
            //update once.
            if (!e.IsSelected || dBListView.SelectedItems.Count > 1)
                return;

            updateGame();

            selectedListItem = dBListView.SelectedItems[0];
            setRomToPanel(selectedListItem);
        }

        //update panel with Game info
        void setRomToPanel(ListViewItem listViewItem)
        {
            saveSelectedGame = false;
            saveThumbs = false;
            saveDiscs = false;
            savePCSettings = false;
            Game dbRom = listViewItem.Tag as Game;
            selectedGame = dbRom;

            if (dbRom == null)
                return;

            allowChangedEvents = false;
            if (dbRom.ParentEmulator.IsPc())
            {
                if (!tabControl1.TabPages.Contains(pcSettingsTab))
                    tabControl1.TabPages.Insert(1, pcSettingsTab);
            }
            else
                tabControl1.TabPages.Remove(pcSettingsTab);

            //update ThumbGroup
            if (itemThumbs != null)
                itemThumbs.Dispose();
            itemThumbs = new ThumbGroup(dbRom);
            if (tabControl1.SelectedTab == thumbsTab)
            {
                setGameArt(itemThumbs); //load thumbs to panels
                thumbsLoaded = true;
            }
            else
            {
                setGameArt(null);
                thumbsLoaded = false;
            }
            idLabel.Text = dbRom.Id.ToString();
            txt_Title.Text = dbRom.Title;
            txt_company.Text = dbRom.Developer;
            txt_description.Text = dbRom.Description;
            txt_yearmade.Text = dbRom.Year.ToString();
            txt_genre.Text = dbRom.Genre;
            gradeUpDown.Value = dbRom.Grade;

            txt_Manual.Text = itemThumbs.ManualPath;

            //chk_Visible.Checked = dbRom.Visible;
            chk_Favourite.Checked = dbRom.Favourite;

            videoTextBox.Text = dbRom.VideoPreview;

            playCountLabel.Text = dbRom.PlayCount.ToString();
            lastPlayLabel.Text = dbRom.Latestplay.ToShortDateString();

            loadProfileDropdown(dbRom);

            discBindingSource.Clear();

            if (dbRom.Discs.Count > 0)
            {
                int? selectedDisc = dbRom.CurrentDisc.Id;
                //int number = 1;
                foreach (GameDisc disc in dbRom.Discs)
                {
                    //disc.Number = number++;
                    if (disc.Id == selectedDisc)
                        disc.Selected = true;
                    discBindingSource.Add(disc);
                }
            }

            setPCSettings(dbRom);
            allowChangedEvents = true;
        }

        //update Game with panel info and Commit
        void updateGame()
        {
            updateThumbs();
            updateDiscs();
            updatePCSettings();
            if (!saveSelectedGame || selectedGame == null)
                return;
            
            selectedGame.Title = txt_Title.Text;
            if (selectedListItem != null)
                selectedListItem.Text = selectedGame.Title;

            selectedGame.Developer = txt_company.Text;
            selectedGame.Description = txt_description.Text;
            selectedGame.Genre = txt_genre.Text;
            try
            {
                selectedGame.Year = Convert.ToInt32(txt_yearmade.Text);
            }
            catch
            {
                selectedGame.Year = 0;
            }

            selectedGame.Grade = Convert.ToInt32(gradeUpDown.Value);

            //selectedGame.Visible = chk_Visible.Checked;
            selectedGame.Favourite = chk_Favourite.Checked;

            selectedGame.VideoPreview = videoTextBox.Text;

            EmulatorProfile selectedProfile = profileComboBox.SelectedItem as EmulatorProfile;
            if (selectedProfile != null)
                selectedGame.CurrentProfile = selectedProfile;

            for (int x = 0; x < discBindingSource.Count; x++)
            {
                GameDisc disc = (GameDisc)discBindingSource[x];
                if (disc.Selected)
                {
                    selectedGame.CurrentDisc = disc;
                    break;
                }
            }

            EmulatorsCore.Database.BeginTransaction();
            selectedGame.Commit();
            EmulatorsCore.Database.EndTransaction();

            if (itemThumbs != null)
            {
                itemThumbs.ManualPath = txt_Manual.Text;
                itemThumbs.SaveManual();
            }

            saveSelectedGame = false;
        }

        //save all thumbs
        void updateThumbs()
        {
            if (!saveThumbs || itemThumbs == null)
                return;

            itemThumbs.SaveAllThumbs();
            saveThumbs = false;
        }

        void updateDiscs()
        {
            if (!saveDiscs || selectedGame == null)
                return;

            selectedGame.Discs.Clear();
            if (discBindingSource.Count > 0)
            {
                for (int x = 0; x < discBindingSource.Count; x++)
                {
                    GameDisc disc = discBindingSource[x] as GameDisc;
                    if (disc == null)
                        continue;

                    disc.Number = x + 1;
                    selectedGame.Discs.Add(disc);
                }
            }
            EmulatorsCore.Database.BeginTransaction();
            selectedGame.Discs.Commit();
            EmulatorsCore.Database.EndTransaction();
            saveDiscs = false;
        }

        void loadProfileDropdown(Game game)
        {
            bool lAllowEvents = allowChangedEvents;
            allowChangedEvents = false;
            profileComboBox.Items.Clear();
            bool selected = false;
            foreach (EmulatorProfile profile in game.EmulatorProfiles)
            {
                profileComboBox.Items.Add(profile);
                if (!selected && profile.Id == game.CurrentProfile.Id)
                {
                    profileComboBox.SelectedItem = profile; //select Game profile
                    selected = true;
                }
            }

            //profile no longer exists, select default
            if (!selected && profileComboBox.Items.Count > 0)
            {
                profileComboBox.SelectedItem = profileComboBox.Items[0];
                saveSelectedGame = true; //remove reference to deleted profile for tidiness
            }
            allowChangedEvents = lAllowEvents;
        }

        #endregion

        #region Overrides

        public override void SavePanel()
        {
            updateGame();
            clearPanel(); //dispose of images
            base.SavePanel();
        }

        public override void ClosePanel()
        {
            SavePanel(); //otherwise game is not saved when closing as List selection not changed
            if (itemThumbs != null)
                itemThumbs.Dispose();
            if (thumbRetriever != null)
                thumbRetriever.ForceClose();
        }

        public override void UpdatePanel()
        {
            updateGame();
            clearPanel();
            initListView();
            base.UpdatePanel();
        }

        #endregion

        #region Emulator Dropdown

        private void updateEmuBox()
        {
            int prevSelected = -2;
            if (emuComboBox.SelectedItem != null)
                prevSelected = ((ComboBoxItem)emuComboBox.SelectedItem).ID;

            emuComboBox.SelectedIndexChanged -= new EventHandler(emuComboBox_SelectedIndexChanged);
            emuComboBox.BeginUpdate();

            emuComboBox.Items.Clear();
            foreach (ComboBoxItem item in Dropdowns.GetEmuComboBoxItems())
                emuComboBox.Items.Add(item);

            if (emuComboBox.Items.Count > 0)
            {
                int selectedIndex = 0;
                for (int x = 0; x < emuComboBox.Items.Count; x++)
                {
                    if (((ComboBoxItem)emuComboBox.Items[x]).ID == prevSelected)
                    {
                        selectedIndex = x;
                        break;
                    }
                }
                emuComboBox.SelectedIndex = selectedIndex;
            }

            emuComboBox.EndUpdate();
            emuComboBox.SelectedIndexChanged += new EventHandler(emuComboBox_SelectedIndexChanged);
            emuComboBox_SelectedIndexChanged(emuComboBox, new EventArgs());
        }

        void emuComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            updateGame();

            loadingRomsPanel.Visible = true;

            int id = -2;
            if (emuComboBox.SelectedItem != null)
                id = ((ComboBoxItem)emuComboBox.SelectedItem).ID;

            bool selected = false;
            int? selectedGameId = null;
            if (selectedListItem != null)
                selectedGameId = ((Game)selectedListItem.Tag).Id;

            dBListView.BeginUpdate();
            dBListView.SelectedItems.Clear();
            dBListView.Items.Clear();

            for (int x = 0; x < dbGames.Count; x++)
            {
                Game game = (Game)dbGames[x].Tag;
                if (id == -2 || game.ParentEmulator.Id == id)
                {
                    dBListView.Items.Add(dbGames[x]);
                    if (selectedGameId != null && !selected && selectedGameId == game.Id)
                    {
                        selected = true;
                        selectedListItem = dbGames[x];
                        dbGames[x].Selected = true;
                    }
                }
            }

            if (!selected && dBListView.Items.Count > 0)
            {
                selectedListItem = dBListView.Items[0];
                dBListView.Items[0].Selected = true;
            }

            dBListView.EndUpdate();
            loadingRomsPanel.Visible = false;
        }

        #endregion

        #region ToolTip

        void setupToolTip()
        {
            romToolTip.SetToolTip(launchRomButton, "Launch selected game");
            romToolTip.SetToolTip(sendToImportButton, "Send selected game(s) to importer");
            romToolTip.SetToolTip(getRomArtButton, "Update game artwork");
            romToolTip.SetToolTip(delRomButton, "Delete selected games(s)");
            romToolTip.SetToolTip(resetPlayCount, "Reset play count");
            romToolTip.SetToolTip(resetLastPlayed, "Reset last play date");
        }

        #endregion

        #region Buttons

        void launchRomButton_Click(object sender, EventArgs e)
        {
            updateGame();
            if (selectedGame != null)
            {
                LaunchHandler.Instance.StartLaunch(selectedGame);
            }
        }

        void sendToImportButton_Click(object sender, EventArgs e)
        {
            updateGame();

            List<Game> games = new List<Game>();
            foreach (ListViewItem item in dBListView.SelectedItems)
            {
                games.Add(item.Tag as Game);
            }

            sendToImporter(games);            
            dBListView.SelectedItems.Clear();
            UpdatePanel();
        }

        void sendToImporter(IEnumerable<Game> games)
        {
            Importer importer = Importer;
            if (importer == null)
                return;

            BackgroundTaskHandler handler = new BackgroundTaskHandler();
            handler.StatusDelegate = () => { return "sending to Importer..."; };
            handler.ActionDelegate = () =>
                {
                    importer.AddGames(games);
                };
            using (Conf_ProgressDialog progressDlg = new Conf_ProgressDialog(handler))
                progressDlg.ShowDialog();
        }

        void delRomButton_Click(object sender, EventArgs e)
        {
            if (dBListView.SelectedItems.Count == 0)
                return;

            DialogResult dlg = MessageBox.Show(
                "Are you sure you want to delete the selected Game(s) and add them to the ignored files list?",
                "Delete Game(s)?",
                MessageBoxButtons.YesNo );

            if (dlg != DialogResult.Yes)
                return;

            selectedListItem = null;
            saveSelectedGame = false;
            saveThumbs = false;
            savePCSettings = false;
            saveDiscs = false;

            List<Game> games = new List<Game>();
            foreach (ListViewItem item in dBListView.SelectedItems)
                games.Add((Game)item.Tag);

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

            dBListView.SelectedItems.Clear();
            UpdatePanel();
        }

        void resetPlayCount_Click(object sender, EventArgs e)
        {
            if (selectedGame != null)
            {
                playCountLabel.Text = "0";
                selectedGame.PlayCount = 0;
                saveSelectedGame = true;
            }
        }

        void resetLastPlayed_Click(object sender, EventArgs e)
        {
            if (selectedGame != null)
            {
                lastPlayLabel.Text = DateTime.MinValue.ToShortDateString();
                selectedGame.Latestplay = DateTime.MinValue;
                saveSelectedGame = true;
            }
        }

        private void addRomButton_Click(object sender, EventArgs e)
        {
            Game newGame = null;
            using (Wzd_NewRom_Main dlg = new Wzd_NewRom_Main())
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                    newGame = dlg.NewGame;
            }

            if (newGame != null)
            {
                newGame.Commit();
                if (!newGame.InfoChecked)
                {
                    newGame.SearchTitle = newGame.Title; //importer will reset title so store user entered title
                    sendToImporter(new Game[] { newGame });
                }
                else //set selectedListItem to new game so it will be selected after list refresh
                    selectedListItem = new ListViewItem(newGame.Title) { Tag = newGame };
                UpdatePanel();
            }
        }

        private void btnNewManual_Click(object sender, EventArgs e)
        {
            if (selectedGame == null)
                return;

            string filter = "PDF | *.pdf";

            string initialDirectory;
            if (txt_Manual.Text != "" && txt_Manual.Text.LastIndexOf("\\") > -1)
                initialDirectory = txt_Manual.Text.Remove(txt_Manual.Text.LastIndexOf("\\"));
            else
                initialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            using (OpenFileDialog dlg = MP1Utils.OpenFileDialog("Select manual", filter, initialDirectory))
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                    txt_Manual.Text = dlg.FileName;
            }
        }

        private void btnManual_Click(object sender, EventArgs e)
        {
            if (selectedGame == null)
                return;

            if (string.IsNullOrEmpty(txt_Manual.Text))
                return;

            if (!txt_Manual.Text.ToLower().EndsWith(".pdf") || !System.IO.File.Exists(txt_Manual.Text))
                return;

            using (System.Diagnostics.Process proc = new System.Diagnostics.Process())
            {
                proc.StartInfo = new System.Diagnostics.ProcessStartInfo(txt_Manual.Text);
                proc.Start();
            }
        }

        Conf_ThumbRetriever thumbRetriever = null;
        private void getRomArtButton_Click(object sender, EventArgs e)
        {
            if (selectedGame == null)
                return;

            if (thumbRetriever == null)
                thumbRetriever = new Conf_ThumbRetriever(selectedGame);
            else
                thumbRetriever.Reset(selectedGame);

            if (thumbRetriever.Visible)
                thumbRetriever.Activate();
            else
                thumbRetriever.Show();
        }

        private void videoButton_Click(object sender, EventArgs e)
        {
            if (selectedGame == null)
                return;

            string filter = "All files (*.*) | *.*";
            string initialDirectory;

            if (System.IO.File.Exists(videoTextBox.Text))
                initialDirectory = System.IO.Directory.GetParent(videoTextBox.Text).FullName;
            else
                initialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            using (OpenFileDialog dlg = MP1Utils.OpenFileDialog("Select path to preview video", filter, initialDirectory))
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                    videoTextBox.Text = dlg.FileName;
            }
        }
        #endregion

        #region Discs
        private void newDiscButton_Click(object sender, EventArgs e)
        {
            if (selectedGame == null)
                return;

            string filter = selectedGame.ParentEmulator.Filter;
            string filterStr = string.Format("{0} rom ({1}) | {2}|All files (*.*) | *.*", selectedGame.ParentEmulator.Title, filter.Replace(";", ", "), filter);

            using (OpenFileDialog dlg = MP1Utils.OpenFileDialog("Select file", filterStr, System.IO.Path.GetDirectoryName(selectedGame.CurrentDisc.Path)))
            {
                dlg.Multiselect = true;
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    foreach (string filename in dlg.FileNames)
                    {
                        GameDisc newDisc = new GameDisc(filename);
                        int index = discBindingSource.Add(newDisc);
                        newDisc.Number = index + 1;
                    }
                    saveDiscs = true;
                }
            }
        }

        private void delDiscButton_Click(object sender, EventArgs e)
        {
            if (discGridView.SelectedRows.Count < 1 || discBindingSource.Count == 1)
                return;

            if (MessageBox.Show("Are you sure you want to delete the selected discs\r\nand add them to the ignored files list?", "Delete discs?", MessageBoxButtons.YesNo) != DialogResult.Yes)
                return;

            List<GameDisc> discs = new List<GameDisc>();
            List<int> indices = new List<int>();
            bool currentDisc = false;

            for (int x = 0; x < discGridView.SelectedRows.Count; x++)
            {
                DataGridViewRow row = discGridView.SelectedRows[x];
                if (!currentDisc)
                    currentDisc = ((GameDisc)row.DataBoundItem).Selected;
                indices.Add(row.Index);
            }

            int selectedIndex = indices.Count > 0 ? indices[0] : 0;

            EmulatorsCore.Database.ExecuteTransaction(indices, index =>
            {
                GameDisc disc = discGridView.Rows[index].DataBoundItem as GameDisc;
                if (disc != null)
                {
                    EmulatorsCore.Options.AddIgnoreFile(disc.Path);
                    disc.Delete();
                }
                discBindingSource.RemoveAt(index);
            });
            
            if (discBindingSource.Count > 0)
            {
                updateDiscNumbers();
                if (selectedIndex > discBindingSource.Count - 1)
                    selectedIndex = discBindingSource.Count - 1;

                DataGridViewRow row = discGridView.Rows[selectedIndex];
                row.Selected = true;
                if (currentDisc)
                    ((GameDisc)row.DataBoundItem).Selected = true;
            }

            saveDiscs = true;
        }

        void updateDiscNumbers()
        {
            for (int x = 0; x < discBindingSource.Count; x++)
            {
                ((GameDisc)discBindingSource[x]).Number = x + 1;
            }
        }

        private void discUpButton_Click(object sender, EventArgs e)
        {
            if (discGridView.SelectedRows.Count != 1)
                return;

            moveDisc(discGridView.SelectedRows[0], -1);
        }

        private void discDownButton_Click(object sender, EventArgs e)
        {
            if (discGridView.SelectedRows.Count != 1)
                return;

            moveDisc(discGridView.SelectedRows[0], 1);
        }

        void moveDisc(DataGridViewRow selectedRow, int increment)
        {
            int index = selectedRow.Index;
            object item = selectedRow.DataBoundItem;

            if (increment == 0 || (increment < 0 && index == 0) || (increment > 0 && index > discBindingSource.Count - 2))
                return;
                        
            discBindingSource.RemoveAt(index);
            index = index + increment;
            discBindingSource.Insert(index, item);
            discGridView.ClearSelection();

            selectedRow = discGridView.Rows[index];
            selectedRow.Selected = true;

            updateDiscNumbers();
            saveDiscs = true;
        }

        void discGridView_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex != discCurrentColumn.Index)
                return;

            if (((GameDisc)discGridView.Rows[e.RowIndex].DataBoundItem).Selected)
                return;

            for (int x = 0; x < discBindingSource.Count; x++)
                ((GameDisc)discBindingSource[x]).Selected = x == e.RowIndex;

            saveSelectedGame = true;
            discGridView.Refresh();
        }
        #endregion

        #region PC Settings
        EmulatorProfile currentPCProfile = null;
        void setPCSettings(Game selectedGame)
        {
            if(!selectedGame.ParentEmulator.IsPc())
                return;

            pcProfileComboBox.Items.Clear();
            bool selected = false;
            foreach (EmulatorProfile profile in selectedGame.GameProfiles)
            {
                pcProfileComboBox.Items.Add(profile);
                if (!selected && profile.Id == selectedGame.CurrentProfile.Id)
                {
                    selected = true;
                    pcProfileComboBox.SelectedItem = profile;
                }
            }
            if (!selected && pcProfileComboBox.Items.Count > 0)
                pcProfileComboBox.SelectedIndex = 0;
            setProfileToPanel(pcProfileComboBox.SelectedItem as EmulatorProfile);
        }

        void setProfileToPanel(EmulatorProfile selectedProfile)
        {
            currentPCProfile = selectedProfile;
            updatePCSettingsButtons();
            if (selectedProfile == null)
                return;
            suspendMPCheckBox.Checked = selectedProfile.SuspendMP;
            argumentsTextBox.Text = selectedProfile.Arguments;
            launchedFileTextBox.Text = selectedProfile.LaunchedExe;
            preCommandText.Text = selectedProfile.PreCommand;
            preCommandWaitCheck.Checked = selectedProfile.PreCommandWaitForExit;
            preCommandWindowCheck.Checked = selectedProfile.PreCommandShowWindow;
            postCommandText.Text = selectedProfile.PostCommand;
            postCommandWaitCheck.Checked = selectedProfile.PostCommandWaitForExit;
            postCommandWindowCheck.Checked = selectedProfile.PostCommandShowWindow;
        }

        void updatePCSettings()
        {
            if (!savePCSettings || currentPCProfile == null)
                return;
            savePCSettings = false;
            currentPCProfile.SuspendMP = suspendMPCheckBox.Checked;
            currentPCProfile.Arguments = argumentsTextBox.Text;
            currentPCProfile.LaunchedExe = launchedFileTextBox.Text;
            currentPCProfile.PreCommand = preCommandText.Text;
            currentPCProfile.PreCommandWaitForExit = preCommandWaitCheck.Checked;
            currentPCProfile.PreCommandShowWindow = preCommandWindowCheck.Checked;
            currentPCProfile.PostCommand = postCommandText.Text;
            currentPCProfile.PostCommandWaitForExit = postCommandWaitCheck.Checked;
            currentPCProfile.PostCommandShowWindow = postCommandWindowCheck.Checked;
            currentPCProfile.Commit();
        }

        void pcProfileComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!allowChangedEvents)
                return;
            allowChangedEvents = false;
            updatePCSettings();
            setProfileToPanel(pcProfileComboBox.SelectedItem as EmulatorProfile);
            allowChangedEvents = true;
        }

        void renameProfileButton_Click(object sender, EventArgs e)
        {
            if (selectedGame == null)
                return;
            updatePCSettings();
            if (currentPCProfile == null)
                return;
            if (currentPCProfile.IsDefault)
            {
                Logger.LogDebug("Default profile cannot be renamed");
                return;
            }

            using (Conf_NewProfile profileDlg = new Conf_NewProfile(selectedGame.ParentEmulator, currentPCProfile))
            {
                if (profileDlg.ShowDialog() == DialogResult.OK)
                {
                    profileDlg.EmulatorProfile.Commit();
                    int index = pcProfileComboBox.SelectedIndex;
                    pcProfileComboBox.Items.Remove(currentPCProfile);
                    pcProfileComboBox.Items.Insert(index, profileDlg.EmulatorProfile);
                    pcProfileComboBox.SelectedIndex = index;
                }
            }
            loadProfileDropdown(selectedGame);
        }

        void addProfileButton_Click(object sender, EventArgs e)
        {
            if (selectedGame == null)
                return;
            updatePCSettings();
            EmulatorProfile newProfile;
            using (Conf_NewProfile profileDlg = new Conf_NewProfile(selectedGame.ParentEmulator, null))
            {
                if (profileDlg.ShowDialog() != DialogResult.OK || profileDlg.EmulatorProfile == null)
                    return;
                newProfile = profileDlg.EmulatorProfile;
            }

            newProfile.SuspendMP = true;
            selectedGame.GameProfiles.Add(newProfile);
            saveSelectedGame = true;
            pcProfileComboBox.Items.Add(newProfile);
            pcProfileComboBox.SelectedItem = newProfile;
            loadProfileDropdown(selectedGame);
        }

        void delProfileButton_Click(object sender, EventArgs e)
        {
            if (selectedGame == null)
                return;
            savePCSettings = false;
            int index = pcProfileComboBox.SelectedIndex;
            if (index > -1)
            {
                EmulatorProfile profile = (EmulatorProfile)pcProfileComboBox.Items[index];
                if (profile.IsDefault)
                    return;
                selectedGame.GameProfiles.Remove(profile);
                saveSelectedGame = true;
                pcProfileComboBox.Items.Remove(profile);
                profile.Delete();
                if (index > 0)
                    index = index - 1;
                pcProfileComboBox.SelectedIndex = index;
            }
            loadProfileDropdown(selectedGame);
        }

        void updatePCSettingsButtons()
        {
            if (currentPCProfile == null || currentPCProfile.IsDefault)
            {
                renameProfileButton.Enabled = false;
                delProfileButton.Enabled = false;
            }
            else
            {
                renameProfileButton.Enabled = true;
                delProfileButton.Enabled = true;
            }
        }

        #endregion
    }
}
