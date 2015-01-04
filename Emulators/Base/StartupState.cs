using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Emulators
{
    public enum StartupState
    {
        EMULATORS = 0,
        GROUPS = 1,
        FAVOURITES = 2,
        PCGAMES = 3,
        LASTUSED = 4
    }

    public class StartupStateHandler
    {
        public string Name { get; set; }
        public StartupState Value { get; set; }
    }
}
