﻿using Emulators.MediaPortal2.Models.Dialogs;
using Emulators.MediaPortal2.Settings;
using MediaPortal.Common;
using MediaPortal.Common.Commands;
using MediaPortal.Common.Localization;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Workflow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.MediaPortal2.Actions
{
    class SwitchViewAction : IWorkflowContributor
    {
        public IResourceString DisplayTitle
        {
            get { return LocalizationHelper.CreateResourceString("[Emulators.Dialogs.SwitchView]"); }
        }

        public void Execute()
        {
            var model = EmulatorsWorkflowModel.Instance();
            StartupState currentState = model.StartupState;

            ItemsList items = new ItemsList();
            var startupStates = StartupStateSetting.StartupStates;
            foreach (string key in startupStates.Keys)
            {
                StartupState state = startupStates[key];
                if (state == StartupState.LASTUSED)
                    continue;

                items.Add(new ListItem(Consts.KEY_NAME, LocalizationHelper.CreateResourceString(key))
                {
                    Command = new MethodDelegateCommand(() => { model.StartupState = state; }),
                    Selected = state == currentState
                });
            }
            ListDialogModel.Instance().ShowDialog("[Emulators.Dialogs.SwitchView]", items);
        }

        public void Initialize()
        {

        }

        public bool IsActionEnabled(NavigationContext context)
        {
            return true;
        }

        public bool IsActionVisible(NavigationContext context)
        {
            return context.WorkflowModelId == Guids.WorkflowSatesMain;
        }

        public event ContributorStateChangeDelegate StateChanged;

        public void Uninitialize()
        {

        }
    }
}
