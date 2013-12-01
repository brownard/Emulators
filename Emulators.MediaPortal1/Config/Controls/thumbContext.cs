using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Emulators
{
    public partial class ThumbContext : ContextMenuStrip
    {
        ToolStripMenuItem copyButton;
        ToolStripMenuItem pasteButton;
        ToolStripMenuItem viewButton; 
        ToolStripMenuItem deleteButton;
        public ThumbContext()
        {
            InitializeComponent();

            this.Size = new System.Drawing.Size(171, 90);
            this.Opening += new CancelEventHandler(ThumbContext_Opening);

            copyButton = new ToolStripMenuItem();
            copyButton.Size = new System.Drawing.Size(170, 22);
            copyButton.Text = "Copy";
            copyButton.Click += new EventHandler(copy_Click);
            this.Items.Add(copyButton);

            pasteButton = new ToolStripMenuItem();
            pasteButton.Size = new System.Drawing.Size(170, 22);
            pasteButton.Text = "Paste";
            pasteButton.Click += new EventHandler(paste_Click);
            this.Items.Add(pasteButton);

            // view Thumb
            viewButton = new ToolStripMenuItem();
            viewButton.Size = new System.Drawing.Size(170, 22);
            viewButton.Text = "View Thumb";
            viewButton.Click += new EventHandler(view_Click);
            this.Items.Add(viewButton);

            // delete Thumb
            deleteButton = new ToolStripMenuItem();
            deleteButton.Size = new System.Drawing.Size(170, 22);
            deleteButton.Text = "Delete Thumb";
            deleteButton.Click += new EventHandler(delete_Click);
            this.Items.Add(deleteButton);

            // browse In Explorer
            ToolStripMenuItem browse = new ToolStripMenuItem();
            browse.Size = new System.Drawing.Size(170, 22);
            browse.Text = "Browse in Explorer";
            browse.Click += new EventHandler(browse_Click);
            this.Items.Add(browse);            
        }

        private void copy_Click(object sender, EventArgs e)
        {
            if (panel != null && panel.ThumbGroup != null)
            {
                Clipboard.Clear();
                Image image = panel.ThumbGroup.GetThumb(panel.ThumbType);
                if (image != null)
                {
                    using (MemoryStream imageStream = new MemoryStream())
                    {
                        image.Save(imageStream, System.Drawing.Imaging.ImageFormat.Png);
                        DataObject dataObject = new DataObject();
                        dataObject.SetData("PNG", true, imageStream);
                        Clipboard.SetDataObject(dataObject, true);
                    }
                }
            }
        }

        void paste_Click(object sender, EventArgs e)
        {
            if (panel != null && panel.ThumbGroup != null)
            {
                if (Clipboard.ContainsImage())
                {
                    using (Image clipImage = Clipboard.GetImage())
                    {
                        if (clipImage != null)
                        {
                            panel.ThumbGroup.UpdateThumb(panel.ThumbType, clipImage);
                            panel.BackgroundImage = panel.ThumbGroup.GetThumb(panel.ThumbType);
                        }
                    }
                }
                else if (Clipboard.ContainsFileDropList())
                {
                    try
                    {
                        Image clipImage = Image.FromFile(Clipboard.GetFileDropList()[0]);
                        if (clipImage != null)
                        {
                            panel.ThumbGroup.UpdateThumb(panel.ThumbType, clipImage);
                            panel.BackgroundImage = panel.ThumbGroup.GetThumb(panel.ThumbType);
                        }
                    }
                    catch (Exception) { }
                }
                else if (Clipboard.ContainsData("PNG"))
                {
                    object data = Clipboard.GetData("PNG");
                    try
                    {
                        using (MemoryStream ms = (MemoryStream)data)
                        using (Image clipImage = Image.FromStream(ms))
                        {
                            panel.ThumbGroup.UpdateThumb(panel.ThumbType, clipImage);
                            panel.BackgroundImage = panel.ThumbGroup.GetThumb(panel.ThumbType);
                        }
                    }
                    catch { }
                }
            }
        }

        void browse_Click(object sender, EventArgs e)
        {
            if (panel == null)
                return;

            panel.ThumbGroup.BrowseThumbs(panel.ThumbType);
        }

        void delete_Click(object sender, EventArgs e)
        {
            if (panel == null)
                return;

            panel.ThumbGroup.UpdateThumb(panel.ThumbType, "");
            panel.BackgroundImage = null;
        }

        void view_Click(object sender, EventArgs e)
        {
            if (panel == null)
                return;

            panel.ThumbGroup.LaunchThumb(panel.ThumbType);
        }

        ThumbPanel panel = null;
        void ThumbContext_Opening(object sender, CancelEventArgs e)
        {
            ContextMenuStrip menu = sender as ContextMenuStrip;
            if (menu != null)
            {
                panel = menu.SourceControl as ThumbPanel;
            }
            else
                panel = null;

            pasteButton.Enabled = Clipboard.ContainsImage() || Clipboard.ContainsFileDropList() || Clipboard.ContainsData("PNG");
            if (panel != null && panel.BackgroundImage != null)
            {
                copyButton.Enabled = true;
                viewButton.Enabled = true;
                deleteButton.Enabled = true;
            }
            else
            {
                copyButton.Enabled = false;
                viewButton.Enabled = false;
                deleteButton.Enabled = false;
            }
        }
    }
}
