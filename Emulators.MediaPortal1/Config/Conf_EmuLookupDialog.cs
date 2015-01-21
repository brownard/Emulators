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
    public partial class Conf_EmuLookupDialog : Form
    {
        IEnumerable<Platform> platforms = null;

        Platform selectedPlatform;
        public Platform SelectedPlatform
        {
            get { return selectedPlatform; }
        }

        public Conf_EmuLookupDialog(IEnumerable<Platform> platforms)
        {
            InitializeComponent();
            comboBox1.DisplayMember = "Name";
            this.platforms = platforms;
        }

        private void Conf_EmuLookupDialog_Load(object sender, EventArgs e)
        {
            if (platforms == null)
                return;

            foreach (Platform platform in platforms)
                comboBox1.Items.Add(platform);
            if (comboBox1.Items.Count > 0)
                comboBox1.SelectedItem = comboBox1.Items[0];
        }

        private void button1_Click(object sender, EventArgs e)
        {
            selectedPlatform = (Platform)comboBox1.SelectedItem;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
