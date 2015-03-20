using Emulators.ImageHandlers;
using Emulators.MediaPortal2.Models;
using Emulators.MediaPortal2.Models.Dialogs;
using MediaPortal.Common;
using MediaPortal.Common.Commands;
using MediaPortal.Common.General;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Workflow;
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

        Emulator emulator;
        public Emulator Emulator { get { return emulator; } }

        public EmulatorViewModel(Emulator emulator, EmulatorsMainModel model)
        {
            this.model = model;
            this.emulator = emulator;
            Name = emulator.Title;
            Description = emulator.Description;
            using (ThumbGroup thumbs = new ThumbGroup(emulator))
            {
                FrontCover = thumbs.FrontCoverDefaultPath;
                Fanart = thumbs.FanartDefaultPath;
            }

            Command = new MethodDelegateCommand(() =>
            {
                model.EmulatorSelected(emulator);
            });

            ContextCommand = new MethodDelegateCommand(showContext);

        }

        void showContext()
        {
            ItemsList items = new ItemsList();
            items.Add(new ListItem(Consts.KEY_NAME, "[Emulators.Config.Edit]")
            {
                Command = new MethodDelegateCommand(() => ConfigureEmulatorModel.EditEmulator(emulator))
            });
            ListDialogModel.Instance().ShowDialog(emulator.Title, items);
        }
    }
}
