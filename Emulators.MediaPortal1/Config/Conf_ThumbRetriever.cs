using Emulators.Image_Handlers;
using Emulators.Import;
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
    public partial class Conf_ThumbRetriever : Form
    {
        #region Members

        Game game;
        GameImageDownloader downloader;
        ScraperResult currentResult;
        List<Image> currentImages;
        bool closing;

        #endregion

        #region Ctor

        public Conf_ThumbRetriever(Game game)
        {
            InitializeComponent();
            this.game = game;
            currentImages = new List<Image>();
            resultsComboBox.DisplayMember = "DisplayMember";
            resultsComboBox.DropDownClosed += new EventHandler(resultsComboBox_DropDownClosed);
            resultsComboBox.DropDown += new EventHandler(resultsComboBox_DropDown);
            resultsComboBox.SelectedIndexChanged += new EventHandler(resultsComboBox_SelectedIndexChanged);
            clearStatusInfo();
        }

        #endregion

        #region Form Events

        void Conf_ThumbRetriever_Load(object sender, EventArgs e)
        {
            searchTextBox.Text = game.Title;
            searchButton_Click(this, new EventArgs());
        }

        void Conf_ThumbRetriever_FormClosing(object sender, FormClosingEventArgs e)
        {
            stopAndClear();
            if (e.CloseReason == CloseReason.UserClosing && !closing)
            {
                e.Cancel = true;
                this.Visible = false;
            }
        }

        void searchButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(searchTextBox.Text)) //ensure we have a search term
                return;

            resultsComboBox.Items.Clear();
            progressBar.Visible = true;
            statusLabel.Text = "Searching...";
            initDownloader();
            downloader.Search(searchTextBox.Text, game);
        }

        void resultsComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            toolTip1.SetToolTip(resultsComboBox, resultsComboBox.Text);
        }

        void resultsComboBox_DropDown(object sender, EventArgs e)
        {
            int maxWidth = MP1Utils.GetComboBoxDropDownWidth(resultsComboBox);
            resultsComboBox.DropDownWidth = maxWidth > resultsComboBox.Width ? maxWidth : resultsComboBox.Width;
        }

        void resultsComboBox_DropDownClosed(object sender, EventArgs e)
        {
            ScraperResult result = resultsComboBox.SelectedItem as ScraperResult;
            if (result != null && result != currentResult)
            {
                currentResult = result;
                getThumbs(result);
            }
        }

        void imagePnl_MouseDown(object sender, MouseEventArgs e)
        {
            DoDragDrop((sender as Panel).BackgroundImage, DragDropEffects.Copy);
        }

        #endregion

        #region Downloader Events

        private void downloader_SearchCompleted(object sender, GameSearchCompletedEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(delegate()
                {
                    downloader_SearchCompleted(sender, e);
                }));
                return;
            }

            if (sender != downloader)
                return;

            clearStatusInfo();
            resultsComboBox.Items.Clear();
            if (e.Results == null || e.Results.Count < 1)
            {
                resultsComboBox.Items.Add("No results");
                return;
            }

            currentResult = e.Result ?? e.Results[0];
            foreach (ScraperResult result in e.Results)
                resultsComboBox.Items.Add(result);
            resultsComboBox.SelectedItem = currentResult;
            getThumbs(currentResult);
        }

        private void downloader_PrimaryImageDownloaded(object sender, ImageDownloadedEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(delegate()
                {
                    downloader_PrimaryImageDownloaded(sender, e);
                }));
                return;
            }

            if(sender != downloader)
                return;
            Panel imagePanel = createImagePanel(e.Image);
            coversPanel.Controls.Add(imagePanel);
        }

        private void downloader_SecondaryImageDownloaded(object sender, ImageDownloadedEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(delegate()
                {
                    downloader_SecondaryImageDownloaded(sender, e);
                }));
                return;
            }

            if (sender != downloader)
                return;
            Panel imagePanel = createImagePanel(e.Image);
            screensPanel.Controls.Add(imagePanel);
        }

        private void downloader_ImageDownloadCompleted(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(delegate()
                {
                    downloader_ImageDownloadCompleted(sender, e);
                }));
                return;
            }

            if (sender == downloader)
                clearStatusInfo();
        }

        #endregion

        #region Public Methods

        internal void Reset(Game game)
        {
            stopAndClear();
            resultsComboBox.Items.Clear();
            this.game = game;
            searchTextBox.Text = game.Title;
            searchButton_Click(this, new EventArgs());
        }

        public void ForceClose()
        {
            closing = true;
            Close();
        }

        #endregion

        #region Private Methods
        
        void getThumbs(ScraperResult result)
        {
            stopAndClear();
            progressBar.Visible = true;
            statusLabel.Text = "Downloading thumbs...";
            initDownloader();
            downloader.DownloadImages(result);
        }

        void initDownloader()
        {
            if (downloader == null)
            {
                downloader = new GameImageDownloader(new ScraperProvider());
                downloader.SearchCompleted += downloader_SearchCompleted;
                downloader.PrimaryImageDownloaded += downloader_PrimaryImageDownloaded;
                downloader.SecondaryImageDownloaded += downloader_SecondaryImageDownloaded;
                downloader.ImageDownloadCompleted += downloader_ImageDownloadCompleted;
            }
        }

        Panel createImagePanel(Image image)
        {
            Panel imagePanel = new Panel();
            imagePanel.BackgroundImage = image;
            imagePanel.Height = 88;
            imagePanel.Width = 109;

            imagePanel.BackgroundImageLayout = ImageLayout.Zoom;
            imagePanel.MouseDown += new MouseEventHandler(imagePnl_MouseDown); //add drag event handler
            currentImages.Add(image);
            return imagePanel;
        }

        void clearStatusInfo()
        {
            progressBar.Visible = false;
            statusLabel.Text = "";
        }

        void stopAndClear()
        {
            if (downloader != null)
            {
                downloader.CancelDownload();
                downloader = null;
            }
            coversPanel.Controls.Clear();
            screensPanel.Controls.Clear();
            foreach (Bitmap image in currentImages)
            {
                try { image.Dispose(); }
                catch { }
            }
            currentImages.Clear();
        }

        #endregion
    }
}
