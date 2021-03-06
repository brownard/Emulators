﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Emulators.ImageHandlers;

namespace Emulators
{
    public partial class ThumbPanel : Panel
    {
        public ThumbPanel()
        {
            InitializeComponent();
            ThumbType = ThumbType.FrontCover;

            this.AllowDrop = true;
            this.BackgroundImageLayout = ImageLayout.Zoom;
            this.DoubleClick += new EventHandler(ThumbPanel_DoubleClick);
            this.DragDrop += new DragEventHandler(ThumbPanel_DragDrop);
            this.DragEnter += new DragEventHandler(ThumbPanel_DragEnter);

            this.ContextMenuStrip = new ThumbContext();
        }

        void ThumbPanel_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(Bitmap)))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else if (e.Data.GetDataPresent(DataFormats.FileDrop, false))
            {
                e.Effect = DragDropEffects.All;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        void ThumbPanel_DragDrop(object sender, DragEventArgs e)
        {
            if (ThumbGroup == null)
                return;

            if (e.Data.GetDataPresent(typeof(Bitmap)))
            {
                ThumbGroup.UpdateThumb(ThumbType, (Bitmap)e.Data.GetData(typeof(Bitmap)));
                this.BackgroundImage = ThumbGroup.GetThumb(ThumbType);
            }
            else if (e.Data.GetDataPresent(DataFormats.FileDrop, false))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                ThumbGroup.UpdateThumb(ThumbType, files[0]);
                this.BackgroundImage = ThumbGroup.GetThumb(ThumbType);
            }
        }

        void ThumbPanel_DoubleClick(object sender, EventArgs e)
        {
            if (ThumbGroup == null)
                return;

            ThumbGroup.LaunchThumb(ThumbType);
        }

        public ThumbType ThumbType
        {
            get;
            set;
        }

        ThumbGroup thumbGroup = null;
        public ThumbGroup ThumbGroup
        {
            get { return thumbGroup; }
            set
            {
                thumbGroup = value;
                if (thumbGroup != null)
                    this.BackgroundImage = thumbGroup.GetThumb(ThumbType);
                else
                    this.BackgroundImage = null;
            }
        }
    }
}
