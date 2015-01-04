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
    public partial class Conf_IgnoredFiles : Form
    {
        public Conf_IgnoredFiles()
        {
            InitializeComponent();
        }

        private void Conf_IgnoredFiles_Load(object sender, EventArgs e)
        {
            foreach (string path in EmulatorsCore.Options.IgnoredFiles())
            {
                dataGridView1.Rows.Add(path);
            }
        }

        private void removeButton_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dataGridView1.SelectedRows)
            {
                string path = row.Cells[0].Value as string;
                EmulatorsCore.Options.RemoveIgnoreFile(path);
                dataGridView1.Rows.Remove(row);
            }
        }
    }
}
