using MediaPortal.Common.Configuration.ConfigurationClasses;
using MediaPortal.Common.Localization;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.MediaPortal2.Settings
{
    public class StartupStateSetting : SingleSelectionList
    {
        static readonly ReadOnlyCollection<string> startupStates = new List<string>(Enum.GetNames(typeof(StartupState))).AsReadOnly();

        public override void Load()
        {
            string startupState = EmulatorsCore.Options.ReadOption(o => o.StartupState).ToString();
            _items = startupStates.Select(LocalizationHelper.CreateResourceString).ToList();
            int selected = startupStates.IndexOf(startupState);
            if (selected < 0)
                selected = 0;
            Selected = selected;
        }

        public override void Save()
        {
            StartupState newState = (StartupState)Enum.Parse(typeof(StartupState), startupStates[Selected]);
            EmulatorsCore.Options.WriteOption(o => o.StartupState = newState);
        }
    }
}
