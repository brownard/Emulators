using Emulators.MediaPortal2.Models.Dialogs;
using Emulators.MediaPortal2.Settings;
using MediaPortal.Common;
using MediaPortal.Common.Commands;
using MediaPortal.Common.Localization;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Workflow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.MediaPortal2.Actions
{
    class ChangeLayoutAction : IWorkflowContributor
    {
        public IResourceString DisplayTitle
        {
            get { return LocalizationHelper.CreateResourceString("[Emulators.Dialogs.ChangeLayout]"); }
        }

        public void Execute()
        {
            var model = EmulatorsWorkflowModel.Instance();
            LayoutType currentType = model.LayoutType;

            ItemsList items = new ItemsList();
            items.Add(new ListItem(Consts.KEY_NAME, "List")
            {
                Command = new MethodDelegateCommand(() => { model.LayoutType = LayoutType.List; }),
                Selected = currentType == LayoutType.List
            });

            items.Add(new ListItem(Consts.KEY_NAME, "Grid")
            {
                Command = new MethodDelegateCommand(() => { model.LayoutType = LayoutType.Icons; }),
                Selected = currentType == LayoutType.Icons
            });

            items.Add(new ListItem(Consts.KEY_NAME, "Covers")
            {
                Command = new MethodDelegateCommand(() => { model.LayoutType = LayoutType.Cover; }),
                Selected = currentType == LayoutType.Cover
            });

            ListDialogModel.Instance().ShowDialog("[Emulators.Dialogs.ChangeLayout]", items);
        }

        public void Initialize()
        {

        }

        public bool IsActionEnabled(NavigationContext context)
        {
            return true;
        }

        public bool IsActionVisible(NavigationContext context)
        {
            return context.WorkflowModelId == Guids.WorkflowSatesMain;
        }

        public event ContributorStateChangeDelegate StateChanged;

        public void Uninitialize()
        {

        }
    }
}
