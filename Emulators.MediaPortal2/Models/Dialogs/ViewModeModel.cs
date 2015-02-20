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
        const string KEY_STARTUP_STATE = "StartupState";
        ItemsList items;

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
            var model = EmulatorsWorkflowModel.Instance();
            model.StartupState = startupState;
        }

        protected void UpdateSelectedFlag()
        {
            var model = EmulatorsWorkflowModel.Instance();
            StartupState currentState = model.StartupState;
            foreach (ListItem item in items)
            {
                object state;
                if (item.AdditionalProperties.TryGetValue(KEY_STARTUP_STATE, out state))
                    item.Selected = (StartupState)state == currentState;
            }
        }

        public ItemsList Items 
        { 
            get 
            {
                UpdateSelectedFlag();
                return items; 
            } 
        }
    }
}
