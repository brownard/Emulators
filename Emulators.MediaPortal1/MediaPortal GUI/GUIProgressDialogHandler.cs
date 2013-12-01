using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPortal.Dialogs;
using MediaPortal.GUI.Library;

namespace Emulators.MediaPortal1
{
    class GUIProgressDialogHandler : IDisposable
    {
        public event EventHandler OnCompleted;

        IBackgroundTask handler;
        GUIDialogProgress dlgPrgrs = null;
        public GUIProgressDialogHandler(IBackgroundTask handler)
        {
            this.handler = handler;
        }

        public void ShowDialog()
        {
            if (handler == null)
                return;
            handler.OnTaskProgress += setProgress;
            handler.OnTaskCompleted += handler_OnTaskCompleted;
            closeProgDialog();
            dlgPrgrs = (GUIDialogProgress)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_PROGRESS);
            if (dlgPrgrs != null)
            {
                dlgPrgrs.Reset();
                dlgPrgrs.DisplayProgressBar = true;
                dlgPrgrs.ShowWaitCursor = false;
                dlgPrgrs.DisableCancel(true);
                dlgPrgrs.SetHeading(string.IsNullOrEmpty(handler.TaskName) ? Options.Instance.GetStringOption("shownname") : handler.TaskName);
                dlgPrgrs.SetLine(1, "");
                dlgPrgrs.StartModal(GUIWindowManager.ActiveWindow);
            }
            else
            {
                GUIWaitCursor.Init(); GUIWaitCursor.Show();
            }
            if (!handler.Start())
            {
                closeProgDialog();
                return;
            }
        }

        void handler_OnTaskCompleted(object sender, EventArgs e)
        {
            closeProgDialog();
            if (OnCompleted != null)
                OnCompleted(this, EventArgs.Empty);
        }

        void setProgress(object sender, ITaskEventArgs e)
        {
            if (dlgPrgrs != null)
            {
                dlgPrgrs.SetPercentage(e.Percent);
                dlgPrgrs.SetLine(1, e.Info);
            }
        }

        void closeProgDialog()
        {
            if (dlgPrgrs != null)
            {
                dlgPrgrs.Close();
                dlgPrgrs.Dispose();
                dlgPrgrs = null;
            }
            else
                GUIWaitCursor.Hide();
        }

        #region IDisposable Members

        public void Dispose()
        {
            closeProgDialog();
        }

        #endregion
    }
}
