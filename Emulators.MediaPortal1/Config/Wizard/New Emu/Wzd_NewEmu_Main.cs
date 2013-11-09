using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Emulators
{
    public partial class Wzd_NewEmu_Main : Wzd_Main
    {
        public Emulator NewEmulator { get; set; }
        public Image Logo { get; set; }
        public Image Fanart { get; set; }

        public Wzd_NewEmu_Main()
        {
            InitializeComponent();
            this.Text = "New Emulator";
            panels = new List<WzdPanel>(new WzdPanel[] { new Wzd_NewEmu_Start(this), new Wzd_NewEmu_Info(this), new Wzd_NewEmu_Config(this), new Wzd_NewEmu_Config2(this), new Wzd_NewEmu_Roms(this) });
            panel1.Controls.Add(panels[0]);
            updateButtons();
        }

    }
}
