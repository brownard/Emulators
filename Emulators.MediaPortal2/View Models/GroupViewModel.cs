using MediaPortal.Common.Commands;
using MediaPortal.Common.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.MediaPortal2
{
    public class GroupViewModel : ItemViewModel
    {
        EmulatorsWorkflowModel model;

        public RomGroup Group { get; private set; }

        public GroupViewModel(RomGroup group, EmulatorsWorkflowModel model)
        {
            this.model = model;
            Group = group;
            Name = group.Title;
            ThumbGroup thumbGroup = group.ThumbGroup;
            if (thumbGroup != null)
                FrontCover = thumbGroup.FrontCoverDefaultPath;

            Command = new MethodDelegateCommand(() =>
            {
                model.GroupSelected(Group);
            });
        }
    }
}
