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
    public partial class Wzd_NewRom_Main : Wzd_Main
    {
        public Game NewGame { get; set; }

        public Wzd_NewRom_Main()
        {
            InitializeComponent();
            this.Text = "New Game";
            panels = new List<WzdPanel>(new WzdPanel[] { new Wzd_NewRom_Start(this), new Wzd_NewRom_Info(this) });
            panel1.Controls.Add(panels[0]);
            updateButtons();
        }
    }
}
