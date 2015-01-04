using MediaPortal.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Emulators.MediaPortal1
{
    class MediaPortalOptions : Options
    {
        public MediaPortalOptions()
        {
            SavePath = Config.GetFile(Config.Dir.Config, "Emulators_2.xml");
        }
    }
}
