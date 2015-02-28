using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Workflow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.MediaPortal2.Models.Dialogs
{
    public class ProfileSelectModel : ListDialogBase, IWorkflowModel
    {
        public ProfileSelectModel()
        {
        }

        void updateState(NavigationContext context)
        {

        }

        public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
        {
            return true;
        }

        public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
        {
            updateState(newContext);
        }

        public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
        {
            
        }

        public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
        {
            updateState(newContext);
        }

        public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
        {
            
        }

        public Guid ModelId
        {
            get { throw new NotImplementedException(); }
        }

        public void Reactivate(NavigationContext oldContext, NavigationContext newContext)
        {
            throw new NotImplementedException();
        }

        public void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
        {
            throw new NotImplementedException();
        }

        public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
        {
            throw new NotImplementedException();
        }
    }
}
