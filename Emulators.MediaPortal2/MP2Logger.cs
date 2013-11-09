using MediaPortal.Common;
using MediaPortal.Common.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Emulators.MediaPortal2
{
    public class MP2Logger : ILog
    {
        const string PREFIX = "[Emulators] ";
        public void LogInfo(string format, params object[] arg)
        {
            ServiceRegistration.Get<ILogger>().Info(PREFIX + format, arg);
        }

        public void LogDebug(string format, params object[] arg)
        {
            ServiceRegistration.Get<ILogger>().Debug(PREFIX + format, arg);
        }

        public void LogWarn(string format, params object[] arg)
        {
            ServiceRegistration.Get<ILogger>().Warn(PREFIX + format, arg);
        }

        public void LogError(string format, params object[] arg)
        {
            ServiceRegistration.Get<ILogger>().Error(PREFIX + format, arg);
        }

        public void LogError(Exception ex)
        {
            ServiceRegistration.Get<ILogger>().Error(ex);
        }
    }
}
