using MediaPortal.Common.Commands;
using MediaPortal.Common.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.MediaPortal2
{
    public class EmulatorViewModel : ItemViewModel
    {
        EmulatorsWorkflowModel model;

        public Emulator Emulator { get; private set; }

        public EmulatorViewModel(Emulator emu, EmulatorsWorkflowModel model)
        {
            this.model = model;
            Emulator = emu;
            Name = emu.Title;
            using (ThumbGroup thumbs = new ThumbGroup(emu))
                FrontCover = thumbs.FrontCoverDefaultPath;

            Command = new MethodDelegateCommand(() =>
            {
                model.EmulatorSelected(Emulator);
            });
        }
    }
}
