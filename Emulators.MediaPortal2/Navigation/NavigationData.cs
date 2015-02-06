using MediaPortal.UI.Presentation.DataObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.MediaPortal2.Navigation
{
    public class NavigationData
    {
        public const string NAVIGATION_DATA = "56D3B892-A862-4928-A8F3-F36DBC904E4F";

        public string DisplayName { get; set; }
        public List<ListItem> ItemsList { get; set; }
        public StartupState StartupState { get; set; }
    }
}
