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
    public class GroupViewModel : ItemViewModel
    {
        EmulatorsMainModel model;

        public RomGroup Group { get; private set; }

        public GroupViewModel(RomGroup group, EmulatorsMainModel model)
        {
            this.model = model;
            Group = group;
            Name = group.Title;
            ThumbGroup thumbGroup = group.ThumbGroup;
            if (thumbGroup != null)
            {
                FrontCover = thumbGroup.FrontCoverDefaultPath;
                Fanart = thumbGroup.FanartDefaultPath;
            }

            Command = new MethodDelegateCommand(() =>
            {
                model.GroupSelected(Group);
            });
        }
    }
}
