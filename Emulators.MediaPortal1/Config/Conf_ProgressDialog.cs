using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Emulators
{
    public partial class Conf_ProgressDialog : Form
    {
        IBackgroundTask handler;

        public Conf_ProgressDialog(IBackgroundTask handler)
        {
            InitializeComponent();
            this.handler = handler;
            if (!string.IsNullOrEmpty(handler.TaskName))
                this.Text = handler.TaskName;
            statusLabel.Text = "";
        }

        void Conf_ProgressDialog_Load(object sender, EventArgs e)
        {
            if (handler == null)
                Close();
            handler.OnTaskProgress += updateStatusInfo;
            handler.OnTaskCompleted += handler_OnTaskCompleted;
            if (!handler.Start())
                Close();
        }

        void handler_OnTaskCompleted(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(delegate()
                {
                    handler_OnTaskCompleted(sender, e);
                }));
                return;
            }
            Close();
        }

        void updateStatusInfo(object sender, ITaskEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(delegate()
                    {
                        updateStatusInfo(sender, e);
                    }));
                return;
            }

            int perc = e.Percent;
            if (perc < 0)
                perc = 0;
            else if (perc > 100)
                perc = 100;
            
            statusLabel.Text = e.Info;
            progressBar.Value = perc;
        }

        void Conf_ProgressDialog_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (handler != null)
                handler.Stop();
        }
    }
}
