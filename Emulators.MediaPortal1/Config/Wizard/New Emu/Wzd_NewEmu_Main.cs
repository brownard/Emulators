using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Emulators.PlatformImporter;
using Emulators.ImageHandlers;

namespace Emulators
{
    public partial class Wzd_NewEmu_Main : Wzd_Main
    {
        public Emulator NewEmulator { get; set; }
        public SafeImage Logo { get; set; }
        public SafeImage Fanart { get; set; }

        public Wzd_NewEmu_Main(IPlatformImporter platformImporter)
        {
            InitializeComponent();
            this.Text = "New Emulator";
            panels = new List<WzdPanel>(new WzdPanel[] { new Wzd_NewEmu_Start(this), new Wzd_NewEmu_Info(this, platformImporter), new Wzd_NewEmu_Config(this), new Wzd_NewEmu_Config2(this), new Wzd_NewEmu_Roms(this) });
            panel1.Controls.Add(panels[0]);
            updateButtons();
        }

    }
}
