﻿namespace Emulators
{
    partial class Wzd_NewEmu_Main
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
                if (Logo != null)
                {
                    Logo.Dispose();
                    Logo = null;
                }
                if (Fanart != null)
                {
                    Fanart.Dispose();
                    Fanart = null;
                }
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // Wzd_NewEmu_Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(386, 328);
            this.Name = "Wzd_NewEmu_Main";
            this.ResumeLayout(false);

        }

        #endregion
    }
}
