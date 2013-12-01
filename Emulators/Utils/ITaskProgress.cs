using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Emulators
{
    public interface IBackgroundTask
    {
        event EventHandler<ITaskEventArgs> OnTaskProgress;
        event EventHandler OnTaskCompleted;
        bool Start();
        void Stop();
        string TaskName { get; }
    }

    public class ITaskEventArgs : EventArgs
    {
        public int Percent { get; private set; }
        public string Info { get; private set; }
        public ITaskEventArgs(int percent, string info)
        {
            Percent = percent;
            Info = info;
        }
    }
}
