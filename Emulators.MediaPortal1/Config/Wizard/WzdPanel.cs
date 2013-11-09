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
    internal partial class WzdPanel : ContentPanel
    {
        public WzdPanel()
        {
            InitializeComponent();
        }

        public virtual bool Next() { return true; }
        public virtual bool Back() { return true; }
    }
}
