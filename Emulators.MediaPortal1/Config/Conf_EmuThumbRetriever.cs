using Emulators.ImageHandlers;
using Emulators.PlatformImporter;
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
    public partial class Conf_EmuThumbRetriever : Form
    {
        #region Members

        Emulator emu = null;
        IPlatformImporter platformImporter;
        EmulatorImageDownloader downloader;
        Platform currentPlatform;
        List<Image> currentImages;
        bool closing;

        #endregion

        #region Ctor

        public Conf_EmuThumbRetriever(Emulator emu, IPlatformImporter platformImporter)
        {
            InitializeComponent();
            this.emu = emu;
            this.platformImporter = platformImporter;
            currentImages = new List<Image>();
            resultsComboBox.DisplayMember = "Name"; 
            resultsComboBox.DropDownClosed += new EventHandler(resultsComboBox_DropDownClosed);            
            clearStatusInfo();
        }

        #endregion

        #region Form Events

        //On first load set search term and start searching
        private void Conf_ThumbRetriever_Load(object sender, EventArgs e)
        {
            getPlatforms();
        }

        private void Conf_ThumbRetriever_FormClosing(object sender, FormClosingEventArgs e)
        {
            stopAndClear();
            //if the user has requested close and the request has not come from the main config form,
            //just hide the form so it can be re-used
            if (e.CloseReason == CloseReason.UserClosing && !closing)
            {
                e.Cancel = true;
                this.Visible = false;
            }
        }

        //called when an item is selected in the results box, update the romMatch
        //with the selected result and start downloading thumbs
        void resultsComboBox_DropDownClosed(object sender, EventArgs e)
        {
            Platform selectedPlatform = resultsComboBox.SelectedItem as Platform;
            if (selectedPlatform != null && selectedPlatform != currentPlatform)
            {
                currentPlatform = selectedPlatform;
                stopAndClear();
                getThumbs(selectedPlatform.Id);
            }
        }

        //allows drag n drop to be used on doenloaded images
        void imagePnl_MouseDown(object sender, MouseEventArgs e)
        {
            DoDragDrop((sender as Panel).BackgroundImage, DragDropEffects.Copy);
        }

        #endregion

        #region Downloader Events

        void downloader_SearchCompleted(object sender, PlatformSearchCompletedEventArgs e)
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
            foreach (Platform platform in e.Platforms)
                resultsComboBox.Items.Add(platform);

            currentPlatform = e.SelectedPlatform;
            if (currentPlatform != null)
            {
                resultsComboBox.SelectedItem = currentPlatform;
                getThumbs(currentPlatform.Id);
            }
        }

        void downloader_PrimaryImageDownloaded(object sender, ImageDownloadedEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(delegate()
                {
                    downloader_PrimaryImageDownloaded(sender, e);
                }));
                return;
            }

            if (sender != downloader)
                return;
            Panel imagePanel = createImagePanel(e.Image);
            coversPanel.Controls.Add(imagePanel);
        }

        void downloader_SecondaryImageDownloaded(object sender, ImageDownloadedEventArgs e)
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

        void downloader_ImageDownloadCompleted(object sender, EventArgs e)
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

        //resets the form, updates the game to search against and starts searching
        internal void Reset(Emulator emu)
        {
            stopAndClear();
            resultsComboBox.Items.Clear();
            this.emu = emu;
            getPlatforms();
        }

        //Called by the main config window, ensure form is closed properly
        //and not just hidden
        public void ForceClose()
        {
            closing = true;
            Close();
        }

        #endregion

        #region Private Methods

        void getPlatforms()
        {
            progressBar.Visible = true;
            statusLabel.Text = "Searching...";
            initDownloader();
            downloader.Search(emu.Platform);
        }

        void getThumbs(string selectedId)
        {
            progressBar.Visible = true;
            statusLabel.Text = "Downloading thumbs...";
            initDownloader();
            downloader.DownloadImages(selectedId);
        }

        void initDownloader()
        {
            if (downloader == null)
            {
                downloader = new EmulatorImageDownloader(platformImporter);
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
            imagePanel.Width = 107;

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
            foreach (Image image in currentImages)
            {
                try { image.Dispose(); }
                catch { }
            }
            currentImages.Clear();
        }

        #endregion
    }
}
