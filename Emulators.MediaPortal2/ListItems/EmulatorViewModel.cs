using Emulators.ImageHandlers;
using Emulators.MediaPortal2.Models;
using MediaPortal.Common.Commands;
using MediaPortal.Common.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.MediaPortal2.ListItems
{
    public class EmulatorViewModel : ItemViewModel
    {
        EmulatorsMainModel model;

        public Emulator Emulator { get; private set; }

        public EmulatorViewModel(Emulator emu, EmulatorsMainModel model)
        {
            this.model = model;
            Emulator = emu;
            Name = emu.Title;
            Description = emu.Description;
            using (ThumbGroup thumbs = new ThumbGroup(emu))
            {
                FrontCover = thumbs.FrontCoverDefaultPath;
                Fanart = thumbs.FanartDefaultPath;
            }

            Command = new MethodDelegateCommand(() =>
            {
                model.EmulatorSelected(Emulator);
            });
        }
    }
}
