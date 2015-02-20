using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Threading;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.MediaPortal2.Models.Dialogs
{
    public class ProgressDialogModel : IWorkflowModel
    {
        protected AbstractProperty _headerProperty;
        protected AbstractProperty _progressProperty;
        protected AbstractProperty _infoProperty;

        IWork currentBackgroundTask;

        public ProgressDialogModel()
        {
            _progressProperty = new WProperty(typeof(int), 0);
            _infoProperty = new WProperty(typeof(string), null);
            _headerProperty = new WProperty(typeof(string), null);
        }

        protected virtual void DoTask()
        {

        }

        protected void SetProgress(string info, int percent)
        {
            Info = info;
            Progress = percent;
        }

        void onEnterContext(NavigationContext context)
        {
            currentBackgroundTask = ServiceRegistration.Get<IThreadPool>().Add(
                DoTask, a => closeDialog(context));
        }

        void closeDialog(NavigationContext context)
        {
            currentBackgroundTask = null;
            var screenMgr = ServiceRegistration.Get<IScreenManager>();
            if (screenMgr.TopmostDialogInstanceId == context.DialogInstanceId)
                screenMgr.CloseTopmostDialog();
        }

        public AbstractProperty HeaderProperty { get { return _headerProperty; } }
        public string Header
        {
            get { return (string)_headerProperty.GetValue(); }
            set { _headerProperty.SetValue(value); }
        }

        public AbstractProperty ProgressProperty { get { return _progressProperty; } }
        public int Progress
        {
            get { return (int)_progressProperty.GetValue(); }
            set { _progressProperty.SetValue(value); }
        }

        public AbstractProperty InfoProperty { get { return _infoProperty; } }
        public string Info
        {
            get { return (string)_infoProperty.GetValue(); }
            set { _infoProperty.SetValue(value); }
        }

        #region Workflow
        public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
        {
            return true;
        }

        public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
        {

        }

        public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
        {

        }

        public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
        {
            onEnterContext(newContext);
        }

        public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
        {
            while (currentBackgroundTask != null)
                System.Threading.Thread.Sleep(100);
        }

        public virtual Guid ModelId { get { return Guid.Empty; } }

        public void Reactivate(NavigationContext oldContext, NavigationContext newContext)
        {

        }

        public void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
        {
        }

        public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
        {
            return ScreenUpdateMode.AutoWorkflowManager;
        }
        #endregion
    }
}
