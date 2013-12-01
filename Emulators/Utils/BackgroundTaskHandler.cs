using Emulators.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Emulators
{  
    public class BackgroundTaskHandler<T> : IBackgroundTask
    {
        public event EventHandler<ITaskEventArgs> OnTaskProgress;
        public event EventHandler OnTaskCompleted;
        public string TaskName { get; set; }

        public List<T> Items { get; set; }
        public Func<T, string> StatusDelegate { get; set; }
        public Action<T> ActionDelegate { get; set; }
        
        System.Threading.Thread worker = null;
        volatile bool doWork = false;

        public void ExecuteProgressHandler(int percent, string info)
        {
            if (OnTaskProgress != null)
                OnTaskProgress(this, new ITaskEventArgs(percent, info));
        }

        public bool Start()
        {
            if (doWork)
                return true;
            doWork = true;
            worker = new System.Threading.Thread(startTask);
            worker.Start();
            return true;
        }

        public void Stop()
        {
            doWork = false;
            if (worker != null)
            {
                if (worker.IsAlive)
                    worker.Join();
                worker = null;
            }
        }

        void startTask()
        {
            if (Items != null && Items.Count > 0)
            {
                int current = 0;
                int total = Items.Count;
                DB.Instance.ExecuteTransaction(Items, item =>
                {
                    if (!doWork)
                        return;

                    string infoTxt = StatusDelegate != null ? StatusDelegate(item) : "";
                    string status = string.Format("{0} / {1} - {2}", current + 1, total, infoTxt);
                    int perc = (int)Math.Round(((double)current / total) * 100);
                    ExecuteProgressHandler(perc, status);
                    ActionDelegate(item);
                    current++;
                });
            }

            if (OnTaskCompleted != null)
                OnTaskCompleted(this, EventArgs.Empty);
        }
    }

    public class BackgroundTaskHandler : IBackgroundTask
    {
        public event EventHandler<ITaskEventArgs> OnTaskProgress;
        public event EventHandler OnTaskCompleted;
        public string TaskName { get; set; }

        public Func<string> StatusDelegate { get; set; }
        public Action ActionDelegate { get; set; }
        System.Threading.Thread worker = null;
        volatile bool doWork = false;

        public bool Start()
        {
            if (doWork)
                return true;
            doWork = true;
            worker = new System.Threading.Thread(startTask);
            worker.Start();
            return true;
        }

        public void Stop()
        {
            doWork = false;
            if (worker != null)
            {
                if (worker.IsAlive)
                    worker.Join();
                worker = null;
            }
        }

        public void ExecuteProgressHandler(string info, int percent)
        {
            if (OnTaskProgress != null)
                OnTaskProgress(this, new ITaskEventArgs(percent, info));
        }

        void startTask()
        {
            if (StatusDelegate != null)
                ExecuteProgressHandler(StatusDelegate(), 0);
            if (ActionDelegate != null)
                ActionDelegate();
            if (OnTaskCompleted != null)
                OnTaskCompleted(this, EventArgs.Empty);
        }
    }
}
