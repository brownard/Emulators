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
    internal partial class Wzd_NewRom_Info : WzdPanel
    {
        Wzd_NewRom_Main parent;
        public Wzd_NewRom_Info(Wzd_NewRom_Main parent)
        {
            InitializeComponent();
            this.parent = parent;
        }

        Emulator currentEmu = null;
        public override void UpdatePanel()
        {
            if (parent.NewGame.ParentEmulator == null)
                profileComboBox.Items.Clear();
            else if (currentEmu == null || currentEmu.Id != parent.NewGame.ParentEmulator.Id)
            {
                currentEmu = parent.NewGame.ParentEmulator;
                profileComboBox.Items.Clear();
                foreach (EmulatorProfile profile in parent.NewGame.EmulatorProfiles)
                    profileComboBox.Items.Add(profile);
                if (profileComboBox.Items.Count > 0)
                    profileComboBox.SelectedItem = profileComboBox.Items[0];
            }

            if (string.IsNullOrEmpty(txt_Title.Text))
                txt_Title.Text = parent.NewGame.Title;
        }

        public override bool Next()
        {
            if (string.IsNullOrEmpty(txt_Title.Text))
            {
                MessageBox.Show("Please enter a title for the new game", "No title", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return false;
            }
            if (profileComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select an emulator profile to use with the new game", "No profile", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return false;
            }

            parent.NewGame.Title = txt_Title.Text;
            parent.NewGame.CurrentProfile = (EmulatorProfile)profileComboBox.SelectedItem;
            parent.NewGame.Favourite = favCheckBox.Checked;
            parent.NewGame.InfoChecked = !importCheckBox.Checked;
            return true;
        }
    }
}
