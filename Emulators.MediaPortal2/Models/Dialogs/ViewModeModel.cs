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
    public class ViewModeModel : ListDialogBase
    {
        const string KEY_STARTUP_STATE = "StartupState";

        public ViewModeModel()
        {
            items = new ItemsList();
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
                    items.Add(item);
                }
            }
        }

        protected void SetStartupState(StartupState startupState)
        {
            var model = EmulatorsMainModel.Instance();
            model.StartupState = startupState;
        }

        protected override void UpdateSelectedFlag()
        {
            var model = EmulatorsMainModel.Instance();
            StartupState currentState = model.StartupState;
            foreach (ListItem item in items)
            {
                object state;
                if (item.AdditionalProperties.TryGetValue(KEY_STARTUP_STATE, out state))
                    item.Selected = (StartupState)state == currentState;
            }
        }
    }
}
