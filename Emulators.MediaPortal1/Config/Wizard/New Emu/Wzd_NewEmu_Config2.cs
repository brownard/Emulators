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
    internal partial class Wzd_NewEmu_Config2 : WzdPanel
    {
        Wzd_NewEmu_Main parent;
        public Wzd_NewEmu_Config2(Wzd_NewEmu_Main parent)
        {
            InitializeComponent();
            this.parent = parent;
        }

        public override void UpdatePanel()
        {
            EmulatorProfile defaultProfile = parent.NewEmulator.DefaultProfile;
            mountImagesCheckBox.Checked = defaultProfile.MountImages;
            escExitCheckBox.Checked = defaultProfile.EscapeToExit;
            suspendMPCheckBox.Checked = defaultProfile.SuspendMP == true;
            enableGMCheckBox.Checked = defaultProfile.EnableGoodmerge;
        }

        public override bool Next()
        {
            EmulatorProfile defaultProfile = parent.NewEmulator.DefaultProfile;
            defaultProfile.MountImages = mountImagesCheckBox.Checked;
            defaultProfile.EscapeToExit = escExitCheckBox.Checked;
            defaultProfile.SuspendMP = suspendMPCheckBox.Checked;
            defaultProfile.EnableGoodmerge = enableGMCheckBox.Checked;
            return true;
        }
    }
}
