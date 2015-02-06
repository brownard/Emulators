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
        public static readonly ReadOnlyDictionary<string, StartupState> StartupStates;
        static StartupStateSetting()
        {
            var states = new Dictionary<string, StartupState>();
            states["[Emulators.LastUsed]"] = StartupState.LASTUSED;
            states["[Emulators.Emulators]"] = StartupState.EMULATORS;
            states["[Emulators.Groups]"] = StartupState.GROUPS;
            states["[Emulators.Favourites]"] = StartupState.FAVOURITES;
            states["[Emulators.PCGames]"] = StartupState.PCGAMES;
            StartupStates = new ReadOnlyDictionary<string, StartupState>(states);
        }

        List<string> items;
        public override void Load()
        {
            StartupState startupState = EmulatorsCore.Options.ReadOption(o => o.StartupState);
            items = new List<string>();
            int selected = 0;
            foreach (var keyVal in StartupStates)
            {
                items.Add(keyVal.Key);
                if (keyVal.Value == startupState)
                    Selected = selected;
                selected++;
            }
            _items = items.Select(LocalizationHelper.CreateResourceString).ToList();
        }

        public override void Save()
        {
            EmulatorsCore.Options.WriteOption(o => o.StartupState = StartupStates[items[Selected]]);
        }
    }
}
