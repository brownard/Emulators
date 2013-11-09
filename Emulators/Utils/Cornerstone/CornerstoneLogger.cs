using Emulators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cornerstone
{
    public class CornerstoneLogger
    {
        public CornerstoneLogger() { }
        const string logAppend = "Emulators2 Cornerstone: ";
        public void Debug(string format, params object[] args)
        {
            Logger.LogDebug(logAppend + string.Format(format, args));
        }
        public void Error(string format, params object[] args)
        {
            Logger.LogError(logAppend + string.Format(format, args));
        }
        public void Error(string message, Exception e)
        {
            Logger.LogError(logAppend + message + " - Exception: " + e.Message);
        }
        public void Warn(string format, params object[] args)
        {
            Logger.LogWarn(logAppend + string.Format(format, args));
        }

        internal void DebugException(string message, Exception e)
        {
            Logger.LogDebug(logAppend + message + " - Exception: " + e.Message);
        }

        internal void ErrorException(string message, Exception e)
        {
            Logger.LogError(logAppend + message + " - Exception: " + e.Message);
        }
    }
}
