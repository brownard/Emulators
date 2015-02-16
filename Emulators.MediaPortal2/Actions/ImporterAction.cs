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
    class ImporterAction : IWorkflowContributor
    {
        public IResourceString DisplayTitle
        {
            get { return LocalizationHelper.CreateResourceString("[Emulators.Importer]"); }
        }

        public void Execute()
        {
            IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
            workflowManager.NavigatePushAsync(Guids.ImporterState);
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
