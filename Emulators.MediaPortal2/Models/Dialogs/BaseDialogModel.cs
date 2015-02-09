using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Emulators.MediaPortal2.Models.Dialogs
{
    public abstract class BaseDialogModel : IWorkflowModel
    {
        public abstract Guid DialogGuid { get; }
        public void Select(ListItem item)
        {
            if (item != null && item.Command != null)
                item.Command.Execute();
        }

        #region Workflow
        public bool CanEnterState(MediaPortal.UI.Presentation.Workflow.NavigationContext oldContext, MediaPortal.UI.Presentation.Workflow.NavigationContext newContext)
        {
            return true;
        }

        public void ChangeModelContext(MediaPortal.UI.Presentation.Workflow.NavigationContext oldContext, MediaPortal.UI.Presentation.Workflow.NavigationContext newContext, bool push)
        {

        }

        public void Deactivate(MediaPortal.UI.Presentation.Workflow.NavigationContext oldContext, MediaPortal.UI.Presentation.Workflow.NavigationContext newContext)
        {

        }

        public void EnterModelContext(MediaPortal.UI.Presentation.Workflow.NavigationContext oldContext, MediaPortal.UI.Presentation.Workflow.NavigationContext newContext)
        {

        }

        public void ExitModelContext(MediaPortal.UI.Presentation.Workflow.NavigationContext oldContext, MediaPortal.UI.Presentation.Workflow.NavigationContext newContext)
        {

        }

        public Guid ModelId
        {
            get { return DialogGuid; }
        }

        public void Reactivate(MediaPortal.UI.Presentation.Workflow.NavigationContext oldContext, MediaPortal.UI.Presentation.Workflow.NavigationContext newContext)
        {

        }

        public void UpdateMenuActions(MediaPortal.UI.Presentation.Workflow.NavigationContext context, IDictionary<Guid, MediaPortal.UI.Presentation.Workflow.WorkflowAction> actions)
        {
        }

        public ScreenUpdateMode UpdateScreen(MediaPortal.UI.Presentation.Workflow.NavigationContext context, ref string screen)
        {
            return ScreenUpdateMode.AutoWorkflowManager;
        }
        #endregion
    }
}
