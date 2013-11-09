using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Emulators.MediaPortal1;

namespace Emulators
{
    internal partial class Wzd_NewRom_Start : WzdPanel
    {
        Wzd_NewRom_Main parent;
        public Wzd_NewRom_Start(Wzd_NewRom_Main parent)
        {
            InitializeComponent();
            initEmuBox();
            this.parent = parent;
        }

        public override bool Next()
        {
            if (emuComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select the emulator to use to launch the game.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return false;
            }

            Emulator emu = (Emulator)((ComboBoxItem)emuComboBox.SelectedItem).Value;
            string parsedPath;
            if (!checkPath(pathTextBox.Text, emu, out parsedPath))
            {
                MessageBox.Show("Please enter a valid path.\r\nIf you are adding a PC game the file must be an exe/bat.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return false;
            }

            //if (Game.Exists(path))
            //{
            //    MessageBox.Show("A game with that path already exists, you cannot have multiple games with the same path.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            //    return false;
            //}
            Game newGame = new Game(emu, pathTextBox.Text);
            if (argsTextBox.Visible)
                newGame.CurrentProfile.Arguments = argsTextBox.Text;
            parent.NewGame = newGame;
            return true;
        }

        private bool checkPath(string path, Emulator emu, out string parsedPath)
        {
            if (emuComboBox.SelectedItem == null)
            {
                parsedPath = null;
                return false;
            }

            if (emu.IsPc())
            {
                if (!path.TryGetExecutablePath(out parsedPath))
                    return false;
            }
            else
            {
                parsedPath = path.Trim();
            }
            return System.IO.File.Exists(parsedPath);
        }

        void initEmuBox()
        {
            bool selected = false;
            emuComboBox.Items.Clear();
            foreach (ComboBoxItem item in Dropdowns.GetNewRomComboBoxItems())
            {
                emuComboBox.Items.Add(item);
                if (!selected && item.ID == Emulator.GetPC().Id)
                {
                    emuComboBox.SelectedItem = item;
                    selected = true;
                }
            }
            if (!selected && emuComboBox.Items.Count > 0)
                emuComboBox.SelectedItem = emuComboBox.Items[0];

            emuComboBox.SelectedIndexChanged += new EventHandler(emuComboBox_SelectedIndexChanged);
            updateArgsVisibilty();
        }

        void emuComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            updateArgsVisibilty();
        }

        void updateArgsVisibilty()
        {
            ComboBoxItem item = emuComboBox.SelectedItem as ComboBoxItem;
            bool visible = item != null && item.ID == Emulator.GetPC().Id;
            argsInfoText.Visible = visible;
            argsLabel.Visible = visible;
            argsTextBox.Visible = visible;
        }

        private void pathBrowseButton_Click(object sender, EventArgs e)
        {
            string filter = "All files (*.*) | *.*";
            string initialDirectory;
            int index = pathTextBox.Text.LastIndexOf("\\");

            if (index > -1)
                initialDirectory = pathTextBox.Text.Remove(index);
            else
                initialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            using (OpenFileDialog dlg = MP1Utils.OpenFileDialog("Path to game", filter, initialDirectory))
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                    pathTextBox.Text = dlg.FileName;
            }
        }
    }
}
