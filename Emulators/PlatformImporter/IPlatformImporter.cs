using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Emulators.PlatformImporter
{
    public interface IPlatformImporter
    {
        ReadOnlyCollection<Platform> GetPlatformList();
        Platform GetPlatformByName(string platformName);
        PlatformInfo GetPlatformInfo(string id);
    }
}
