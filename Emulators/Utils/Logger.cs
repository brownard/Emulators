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
            if (EmulatorsCore.Logger != null)
                EmulatorsCore.Logger.LogInfo(format, args);
        }
        public static void LogDebug(string format, params object[] args)
        {
            if (EmulatorsCore.Logger != null)
                EmulatorsCore.Logger.LogDebug(format, args);
        }
        public static void LogWarn(string format, params object[] args)
        {
            if (EmulatorsCore.Logger != null)
                EmulatorsCore.Logger.LogWarn(format, args);
        }
        public static void LogError(string format, params object[] args)
        {
            if (EmulatorsCore.Logger != null)
                EmulatorsCore.Logger.LogError(format, args);
        }
        public static void LogError(Exception ex)
        {
            if (EmulatorsCore.Logger != null)
                EmulatorsCore.Logger.LogError(ex);
        }
    }
}
