using Emulators.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Emulators
{  
    public class BackgroundTaskHandler<T> : ITaskProgress
    {
        public event BackgroundTaskProgress OnTaskProgress;
        public List<T> Items { get; set; }
        public Func<T, string> StatusDelegate { get; set; }
        public Action<T> ActionDelegate { get; set; }

        volatile bool isComplete = false;
        public bool IsComplete { get { return isComplete; } }

        System.Threading.Thread worker = null;
        volatile bool doWork = false;

        public void ExecuteProgressHandler(int percent, string info)
        {
            if (OnTaskProgress != null)
                OnTaskProgress(percent, info);
        }

        public bool Start()
        {
            if (doWork)
                return true;
            isComplete = false;
            doWork = true;
            worker = new System.Threading.Thread(startTask);
            worker.Start();
            return true;
        }

        public void Stop()
        {
            doWork = false;
            if (worker != null && worker.IsAlive)
            {
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
                isComplete = current == total;
            }
            else
            {
                isComplete = true;
            }
            if (isComplete)
                ExecuteProgressHandler(100, "Complete");
        }
    }

    public class BackgroundTaskHandler : ITaskProgress
    {
        public event BackgroundTaskProgress OnTaskProgress;
        public Func<string> StatusDelegate { get; set; }
        public Action ActionDelegate { get; set; }

        volatile bool isComplete = false;
        public bool IsComplete { get { return isComplete; } }
        System.Threading.Thread worker = null;
        volatile bool doWork = false;

        public bool Start()
        {
            if (doWork)
                return true;
            isComplete = false;
            doWork = true;
            worker = new System.Threading.Thread(startTask);
            worker.Start();
            return true;
        }

        public void Stop()
        {
            doWork = false;
            if (worker != null && worker.IsAlive)
            {
                worker.Join();
                worker = null;
            }
        }

        public void ExecuteProgressHandler(string info, int percent)
        {
            if (OnTaskProgress != null)
                OnTaskProgress(percent, info);
        }

        void startTask()
        {
            if (StatusDelegate != null)
                ExecuteProgressHandler(StatusDelegate(), 0);
            if (ActionDelegate != null)
                ActionDelegate();
            isComplete = true;
            ExecuteProgressHandler("Complete", 100);
        }
    }
}
