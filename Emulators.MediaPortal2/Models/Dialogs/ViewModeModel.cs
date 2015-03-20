using Emulators.MediaPortal2.Settings;
using MediaPortal.Common.Commands;
using MediaPortal.Common.Localization;
using MediaPortal.UI.Presentation.DataObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.MediaPortal2.Models.Dialogs
{
    public class ViewModeModel
    {
        const string KEY_STARTUP_STATE = "EmulatorsStartupState";
        protected ItemsList startupItems;

        public ViewModeModel()
        {
            startupItems = new ItemsList();
            var startupStates = StartupStateSetting.StartupStates;
            foreach (string key in startupStates.Keys)
            {
                StartupState state = startupStates[key];
                if (state != StartupState.LASTUSED)
                {
                    ListItem item = new ListItem(Consts.KEY_NAME, LocalizationHelper.CreateResourceString(key))
                    {
                        Command = new MethodDelegateCommand(() => SetStartupState(state))
                    };
                    item.AdditionalProperties[KEY_STARTUP_STATE] = state;
                    startupItems.Add(item);
                }
            }
        }

        public ItemsList Items
        {
            get { return GetItems(); }
        }

        protected void SetStartupState(StartupState startupState)
        {
            var model = EmulatorsMainModel.Instance();
            model.StartupState = startupState;
        }

        protected ItemsList GetItems()
        {
            var model = EmulatorsMainModel.Instance();
            StartupState currentState = model.StartupState;

            bool showPC = Emulator.GetPC().Games.Count > 0;
            if (!showPC && currentState == StartupState.PCGAMES)
                currentState = StartupState.EMULATORS;

            ItemsList items = new ItemsList();
            foreach (ListItem item in startupItems)
            {
                StartupState state;
                if (tryGetStartupState(item.AdditionalProperties, out state))
                {
                    if (state != StartupState.PCGAMES || showPC)
                    {
                        item.Selected = (StartupState)state == currentState;
                        items.Add(item);
                    }
                }
            }
            return items;
        }

        static bool tryGetStartupState(IDictionary<string, object> properties, out StartupState state)
        {
            object stateOb;
            if (properties.TryGetValue(KEY_STARTUP_STATE, out stateOb))
            {
                state = (StartupState)stateOb;
                return true;
            }
            state = StartupState.EMULATORS;
            return false;
        }
    }
}
