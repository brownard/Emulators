using MediaPortal.Common;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Workflow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Emulators.MediaPortal2
{
    public class ListDialogModel : BaseDialogModel
    {
        public override Guid DialogGuid
        {
            get { return Guids.ListDialogWorkflow; }
        }

        public string Header { get; private set; }
        public ItemsList Items { get; private set; }

        public void ShowDialog(string header, ItemsList items)
        {
            Header = header;
            Items = items;

            var workflowManager = ServiceRegistration.Get<IWorkflowManager>();
            workflowManager.NavigatePushTransientAsync(
                new WorkflowState(Guid.NewGuid(), "emulators_list_dialog", header, true, "DialogList", true, true, DialogGuid, WorkflowType.Dialog),
                new NavigationContextConfig()
                );
        }
    }
}
