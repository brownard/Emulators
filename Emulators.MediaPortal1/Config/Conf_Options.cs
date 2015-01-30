using Emulators.Import;
using Emulators.MediaPortal1;
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
    partial class Conf_Options_New : ContentPanel
    {
        bool updateGeneral = false;
        bool updateLayout = false;
        bool updateStopEmu = false;
        bool updateGoodmerge = false;
        bool updateDatabase = false;
        bool updateAdvanced = false;
        bool updateCommunityServer = false;

        public Conf_Options_New()
        {
            InitializeComponent();            
        }

        void Conf_Options_New_Load(object sender, EventArgs e)
        {
            getOptions();
            keyMapLabel.PreviewKeyDown += new PreviewKeyDownEventHandler(mapKeyDown);
            addEventHandlers();
            if (scraperListBox.Items.Count > 0)
                scraperListBox.SelectedIndex = 0;
        }

        #region Event Handlers

        void addEventHandlers()
        {
            //General options
            shownnameBox.TextChanged += new EventHandler(generalOptionsChanged);
            languageBox.TextChanged += new EventHandler(generalOptionsChanged);
            startupComboBox.TextChanged += new EventHandler(generalOptionsChanged);
            clickToDetailsCheckBox.CheckedChanged += new EventHandler(generalOptionsChanged);
            showSortPropertyBox.CheckedChanged += new EventHandler(generalOptionsChanged);
            stopMediaCheckBox.CheckedChanged += new EventHandler(generalOptionsChanged);

            //Layout options
            viewBox.TextChanged += new EventHandler(layoutOptionsChanged);
            pcViewBox.TextChanged += new EventHandler(layoutOptionsChanged);
            favouritesViewBox.TextChanged += new EventHandler(layoutOptionsChanged);
            showFanArtCheckBox.CheckedChanged += new EventHandler(layoutOptionsChanged);
            fanartDelayBox.ValueChanged += new EventHandler(layoutOptionsChanged);
            showGameArtCheckBox.CheckedChanged += new EventHandler(layoutOptionsChanged);
            gameArtDelayBox.ValueChanged += new EventHandler(layoutOptionsChanged);

            showPrevVideoCheckBox.CheckedChanged += new EventHandler(layoutOptionsChanged);
            prevVidDelayBox.ValueChanged += new EventHandler(layoutOptionsChanged);
            loopPrevVidCheckBox.CheckedChanged += new EventHandler(layoutOptionsChanged);
            useEmuPrevVidCheckBox.CheckedChanged += new EventHandler(layoutOptionsChanged);

            //Stop emu options
            stopEmuCheckBox.CheckedChanged += new EventHandler(stopEmulationOptionsChanged);
            keyMapLabel.TextChanged += new EventHandler(stopEmulationOptionsChanged);

            //Goodmerge options
            goodFiltersTextBox.TextChanged += new EventHandler(goodmergeOptionsChanged);
            showGMDialogCheckBox.CheckStateChanged += new EventHandler(goodmergeOptionsChanged);

            // Community server options
            //submitGameInfoToServer.CheckedChanged += new EventHandler(communityServerOptionsChanged);
            //retrieveGameInfoToServer.CheckedChanged += new EventHandler(communityServerOptionsChanged);
            //communityServerAddress.TextChanged += new EventHandler(communityServerOptionsChanged);
            //communityServerErrorWaitTime.ValueChanged += new EventHandler(communityServerOptionsChanged);

            //Database options
            autoRefreshGames.CheckedChanged += new EventHandler(databaseOptionsChanged);
            autoImportCheckBox.CheckedChanged += new EventHandler(databaseOptionsChanged);
            exactMatchCheckBox.CheckedChanged += new EventHandler(databaseOptionsChanged);
            approveTopCheckBox.CheckedChanged += new EventHandler(databaseOptionsChanged);
            resizeThumbCheckBox.CheckedChanged += new EventHandler(databaseOptionsChanged);
            thoroughThumbCheckBox.CheckedChanged += new EventHandler(databaseOptionsChanged);

            //Advanced options
            thumbDirTextBox.TextChanged += new EventHandler(advancedOptionsChanged);
            threadCountUpDown.ValueChanged += new EventHandler(advancedOptionsChanged);

            scraperListBox.ItemCheck += new ItemCheckEventHandler(advancedOptionsChanged);
            scraperListBox.SelectedIndexChanged += new EventHandler(scraperListBox_SelectedIndexChanged);
            coversScraperComboBox.SelectedIndexChanged += new EventHandler(advancedOptionsChanged);
            screensScraperComboBox.SelectedIndexChanged += new EventHandler(advancedOptionsChanged);
            fanartScraperComboBox.SelectedIndexChanged += new EventHandler(advancedOptionsChanged);
        }

        void scraperListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            scraperUpButton.Enabled = true;
            scraperDownButton.Enabled = true;
            int index = scraperListBox.SelectedIndex;
            if (index == 0)
                scraperUpButton.Enabled = false;
            if (index == scraperListBox.Items.Count - 1)
                scraperDownButton.Enabled = false;
        }

        void communityServerOptionsChanged(object sender, EventArgs e)
        {
            updateCommunityServer = true;
        }

        void advancedOptionsChanged(object sender, EventArgs e)
        {
            updateAdvanced = true;
        }

        void databaseOptionsChanged(object sender, EventArgs e)
        {
            updateDatabase = true;
        }

        void layoutOptionsChanged(object sender, EventArgs e)
        {
            updateLayout = true;
        }

        void generalOptionsChanged(object sender, EventArgs e)
        {
            updateGeneral = true;
        }

        void stopEmulationOptionsChanged(object sender, EventArgs e)
        {
            updateStopEmu = true;
        }

        void goodmergeOptionsChanged(object sender, EventArgs e)
        {
            updateGoodmerge = true;
        }

        #endregion

        void getOptions()
        {
            MP1Options options = MP1Utils.Options;
            options.EnterReadLock();
            shownnameBox.Text = options.PluginDisplayName;
            foreach (string language in Translator.Instance.GetLanguages())
                languageBox.Items.Add(language);
            languageBox.Text = options.Language;

            StartupState selectedValue;
            startupComboBox.DataSource = MP1Utils.GetStartupOptions(out selectedValue);//.ToArray();
            startupComboBox.SelectedValue = selectedValue;

            clickToDetailsCheckBox.Checked = options.ClickToDetails;
            showSortPropertyBox.Checked = options.ShowSortValue;
            stopMediaCheckBox.Checked = options.StopMediaPlayback;

            viewBox.SelectedIndex = options.EmulatorLayout;
            pcViewBox.SelectedIndex = options.PCGamesLayout;
            favouritesViewBox.SelectedIndex = options.FavouritesLayout;
            showFanArtCheckBox.Checked = options.ShowFanart;
            fanartDelayBox.Value = options.FanartDelay;
            showGameArtCheckBox.Checked = options.ShowGameart;
            gameArtDelayBox.Value = options.GameartDelay;

            showPrevVideoCheckBox.Checked = options.ShowVideoPreview;
            prevVidDelayBox.Value = options.PreviewVideoDelay;
            loopPrevVidCheckBox.Checked = options.LoopVideoPreview;
            useEmuPrevVidCheckBox.Checked = options.FallBackToEmulatorVideo;

            stopEmuCheckBox.Checked = options.StopOnMappedKey;
            keyData = options.MappedKey;
            if (keyData > 0)
                keyMapLabel.Text = Options.GetKeyDisplayString(keyData);

            goodFiltersTextBox.Text = options.GoodmergeFilters;

            if (options.ShowGoodmergeDialogOnFirstOpen)
                showGMDialogCheckBox.CheckState = CheckState.Indeterminate;
            else
                showGMDialogCheckBox.CheckState = options.AlwaysShowGoodmergeDialog ? CheckState.Checked : CheckState.Unchecked;

            //submitGameInfoToServer.Checked = options.GetBoolOption("submitGameDetails");
            //retrieveGameInfoToServer.Checked = options.GetBoolOption("retrieveGameDetials");
            //communityServerAddress.Text = options.GetStringOption("communityServerAddress");
            //communityServerErrorWaitTime.Value = options.GetIntOption("communityServerConnectionRetryTime");

            autoRefreshGames.Checked = options.AutoRefreshGames;
            autoImportCheckBox.Checked = options.AutoImportGames;
            exactMatchCheckBox.Checked = options.ImportExact;
            approveTopCheckBox.Checked = options.ImportTop;
            resizeThumbCheckBox.Checked = options.ResizeGameart;
            thoroughThumbCheckBox.Checked = options.TryAndFillMissingArt;

            thumbDirTextBox.Text = options.ImageDirectory;

            //validate threadcount
            int threadCount = options.ImportThreads;
            if (threadCount > 10)
            {
                threadCount = 10;
                updateAdvanced = true;
            }
            else if (threadCount < 1)
            {
                threadCount = 1;
                updateAdvanced = true;
            }
            threadCountUpDown.Value = threadCount;

            //scraper checkbox
            List<string> ignoredScripts = new List<string>();
            string[] optStr = options.IgnoredScrapers.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            ignoredScripts.AddRange(optStr);

            coversScraperComboBox.Items.Add("Use search result");
            coversScraperComboBox.SelectedIndex = 0;
            screensScraperComboBox.Items.Add("Use search result");
            screensScraperComboBox.SelectedIndex = 0;
            fanartScraperComboBox.Items.Add("Use search result");
            fanartScraperComboBox.SelectedIndex = 0;

            foreach (Scraper scraper in ScraperProvider.GetScrapers(true))
            {
                bool scraperEnabled = !ignoredScripts.Contains(scraper.IdString);
                scraperListBox.Items.Add(scraper, scraperEnabled);
                coversScraperComboBox.Items.Add(scraper);
                screensScraperComboBox.Items.Add(scraper);
                int index = fanartScraperComboBox.Items.Add(scraper);
                if (scraper.IdString == options.PriorityCoversScraper)
                    coversScraperComboBox.SelectedIndex = index;
                if (scraper.IdString == options.PriorityScreensScraper)
                    screensScraperComboBox.SelectedIndex = index;
                if (scraper.IdString == options.PriorityFanartScraper)
                    fanartScraperComboBox.SelectedIndex = index;
            }
            options.ExitReadLock();
        }

        public override void SavePanel()
        {
            MP1Options options = MP1Utils.Options;
            options.EnterWriteLock();
            if (updateGeneral)
            {
                options.PluginDisplayName = shownnameBox.Text;
                options.Language = languageBox.Text;
                options.StartupState = (StartupState)startupComboBox.SelectedValue;
                options.ClickToDetails = clickToDetailsCheckBox.Checked;
                options.ShowSortValue = showSortPropertyBox.Checked;
                options.StopMediaPlayback = stopMediaCheckBox.Checked;
                updateGeneral = false;
            }

            if (updateLayout)
            {
                options.EmulatorLayout = viewBox.SelectedIndex;
                options.PCGamesLayout = pcViewBox.SelectedIndex;
                options.FavouritesLayout = favouritesViewBox.SelectedIndex;
                options.FanartDelay = Convert.ToInt32(fanartDelayBox.Value);
                options.ShowFanart = showFanArtCheckBox.Checked;
                options.GameartDelay = Convert.ToInt32(gameArtDelayBox.Value);
                options.ShowGameart = showGameArtCheckBox.Checked;

                options.ShowVideoPreview = showPrevVideoCheckBox.Checked;
                options.PreviewVideoDelay = Convert.ToInt32(prevVidDelayBox.Value);
                options.LoopVideoPreview = loopPrevVidCheckBox.Checked;
                options.FallBackToEmulatorVideo = useEmuPrevVidCheckBox.Checked;

                updateLayout = false;
            }

            if (updateStopEmu)
            {
                if (keyData > 0)
                    options.MappedKey = keyData;
                options.StopOnMappedKey = stopEmuCheckBox.Checked;
                updateStopEmu = false;
            }

            if (updateGoodmerge)
            {
                options.GoodmergeFilters = goodFiltersTextBox.Text;
                if (showGMDialogCheckBox.CheckState == CheckState.Indeterminate)
                {
                    options.ShowGoodmergeDialogOnFirstOpen = true;
                    options.AlwaysShowGoodmergeDialog = false;
                }
                else
                {
                    options.ShowGoodmergeDialogOnFirstOpen = false;
                    options.AlwaysShowGoodmergeDialog = showGMDialogCheckBox.Checked;
                }

                updateGoodmerge = false;
            }

            if (updateDatabase)
            {
                options.AutoRefreshGames = autoRefreshGames.Checked;
                options.AutoImportGames = autoImportCheckBox.Checked;
                options.ImportExact = exactMatchCheckBox.Checked;
                options.ImportTop = approveTopCheckBox.Checked;
                options.ResizeGameart = resizeThumbCheckBox.Checked;
                options.TryAndFillMissingArt = thoroughThumbCheckBox.Checked;
                updateDatabase = false;
            }

            if (updateAdvanced)
            {
                options.ImageDirectory = thumbDirTextBox.Text;
                options.ImportThreads = (int)threadCountUpDown.Value;

                string ignoredScripts = "";
                string scriptPriorities = "";
                for (int x = 0; x < scraperListBox.Items.Count; x++)
                {
                    Scraper scraper = (Scraper)scraperListBox.Items[x];
                    string optStr = scraper.IdString + ";";
                    scriptPriorities += optStr;
                    if (!scraperListBox.GetItemChecked(x))
                        ignoredScripts += optStr;
                }
                options.IgnoredScrapers = ignoredScripts;
                options.ScraperPriorities = scriptPriorities;

                if (coversScraperComboBox.SelectedIndex < 1)
                    options.PriorityCoversScraper = "";
                else
                    options.PriorityCoversScraper = ((Scraper)coversScraperComboBox.SelectedItem).IdString;

                if (screensScraperComboBox.SelectedIndex < 1)
                    options.PriorityScreensScraper = "";
                else
                    options.PriorityScreensScraper = ((Scraper)screensScraperComboBox.SelectedItem).IdString;

                if (fanartScraperComboBox.SelectedIndex < 1)
                    options.PriorityFanartScraper = "";
                else
                    options.PriorityFanartScraper = ((Scraper)fanartScraperComboBox.SelectedItem).IdString;

                updateAdvanced = false;
            }

            if (updateCommunityServer)
            {
                //options.UpdateOption("submitGameDetails", submitGameInfoToServer.Checked);
                //options.UpdateOption("retrieveGameDetials", retrieveGameInfoToServer.Checked);
                //options.UpdateOption("communityServerAddress", communityServerAddress.Text);
                //options.UpdateOption("communityServerConnectionRetryTime", communityServerErrorWaitTime.Value);
            }
            options.ExitWriteLock();
            base.SavePanel();
        }

        public override void ClosePanel()
        {
            SavePanel();
            base.ClosePanel();
        }

        private void thumbDirButton_Click(object sender, EventArgs e)
        {
            string initialDir = thumbDirTextBox.Text;
            if(!System.IO.Directory.Exists(initialDir))
                initialDir = MediaPortal.Configuration.Config.GetFolder(MediaPortal.Configuration.Config.Dir.Thumbs);

            using (FolderBrowserDialog dlg = MP1Utils.OpenFolderDialog("Select thumb directory", initialDir))
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                    thumbDirTextBox.Text = dlg.SelectedPath;
            }
        }

        #region Key Mapping

        bool mapping = false;
        Timer timer = null;

        int keyData = -1;

        void mapKeyDown(object o, PreviewKeyDownEventArgs e)
        {
            if (mapping && e.KeyCode != Keys.ControlKey && e.KeyCode != Keys.ShiftKey && e.KeyCode != Keys.Menu)
            {
                keyData = (int)e.KeyData;
                keyMapLabel.Text = Options.GetKeyDisplayString(keyData); //Enum.GetName(typeof(Keys), e.KeyCode);
                stopMapping();
            }
        }

        private void mapKeyButton_Click(object sender, EventArgs e)
        {
            keyMapLabel.Focus();
            mapping = true;
            mapKeyButton.Text = "Press Key";
            mapKeyButton.Enabled = false;
            timer = new Timer() { Interval = 5000 };
            timer.Tick += new EventHandler(delegate(object o, EventArgs ev)
            {
                stopMapping();
            }
                );
            timer.Start();
        }

        private void stopMapping()
        {
            mapping = false;
            mapKeyButton.Text = "Map";
            mapKeyButton.Enabled = true;
            if (timer != null)
            {
                timer.Stop();
                timer.Dispose();
                timer = null;
            }
        }

        #endregion

        void ignoredFilesButton_Click(object sender, EventArgs e)
        {
            using (Conf_IgnoredFiles dlg = new Conf_IgnoredFiles())
                dlg.ShowDialog();
        }

        void moveScript(int direction)
        {
            object item = scraperListBox.SelectedItem;
            if (item == null)
                return;
            int currIndex = scraperListBox.SelectedIndex;
            int newIndex = currIndex + direction;
            if (newIndex < 0)
                newIndex = 0;
            else if (newIndex > scraperListBox.Items.Count - 1)
                newIndex = scraperListBox.Items.Count - 1;

            if (newIndex == currIndex)
                return;

            bool isChecked = scraperListBox.GetItemChecked(currIndex);
            scraperListBox.Items.RemoveAt(scraperListBox.SelectedIndex);
            scraperListBox.Items.Insert(newIndex, item);
            scraperListBox.SelectedItem = item;
            scraperListBox.SetItemChecked(newIndex, isChecked);
            updateAdvanced = true;
        }

        private void scraperUpButton_Click(object sender, EventArgs e)
        {
            moveScript(-1);
        }

        private void scraperDownButton_Click(object sender, EventArgs e)
        {
            moveScript(1);
        }
    }
}
