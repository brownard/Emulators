using MediaPortal.GUI.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Emulators.MediaPortal1
{
    class MP1Logger : ILog
    {
        const string PREFIX = "Emulators: ";
        public void LogInfo(string format, params object[] arg)
        {
            Log.Info(PREFIX + format, arg);
        }

        public void LogDebug(string format, params object[] arg)
        {
            Log.Debug(PREFIX + format, arg);
        }

        public void LogWarn(string format, params object[] arg)
        {
            Log.Warn(PREFIX + format, arg);
        }

        public void LogError(string format, params object[] arg)
        {
            Log.Error(PREFIX + format, arg);
        }

        public void LogError(Exception ex)
        {
            Log.Error(ex);
        }
    }
}
