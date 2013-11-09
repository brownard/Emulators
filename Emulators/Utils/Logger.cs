using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Emulators
{
    public interface ILog
    {
        void LogInfo(string format, params object[] arg);
        void LogDebug(string format, params object[] arg);
        void LogWarn(string format, params object[] arg);
        void LogError(string format, params object[] arg);
        void LogError(Exception ex);
    }

    public static class Logger
    {
        public static void LogInfo(string format, params object[] args)
        {
            if (Emulators2Settings.Instance.ILogger != null)
                Emulators2Settings.Instance.ILogger.LogInfo(format, args);
        }
        public static void LogDebug(string format, params object[] args)
        {
            if (Emulators2Settings.Instance.ILogger != null)
                Emulators2Settings.Instance.ILogger.LogDebug(format, args);
        }
        public static void LogWarn(string format, params object[] args)
        {
            if (Emulators2Settings.Instance.ILogger != null)
                Emulators2Settings.Instance.ILogger.LogWarn(format, args);
        }
        public static void LogError(string format, params object[] args)
        {
            if (Emulators2Settings.Instance.ILogger != null)
                Emulators2Settings.Instance.ILogger.LogError(format, args);
        }
        public static void LogError(Exception ex)
        {
            if (Emulators2Settings.Instance.ILogger != null)
                Emulators2Settings.Instance.ILogger.LogError(ex);
        }
    }
}
