using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.UI.Presentation.Screens;
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
            IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
            screenManager.ShowDialog(Consts.DIALOG_LAYOUT);
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
            return context.WorkflowModelId == Guids.EmulatorsMainModel;
        }

        public event ContributorStateChangeDelegate StateChanged;

        public void Uninitialize()
        {

        }
    }
}
