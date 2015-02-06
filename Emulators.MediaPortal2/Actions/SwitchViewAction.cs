﻿using Emulators.MediaPortal2.Settings;
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
            get { return LocalizationHelper.CreateResourceString("[Emulators.SwitchView]"); }
        }

        public void Execute()
        {
            var startupStates = StartupStateSetting.StartupStates;
            ItemsList items = new ItemsList();
            foreach (string key in startupStates.Keys)
            {
                string lKey = key;
                items.Add(new ListItem(Consts.KEY_NAME, LocalizationHelper.CreateResourceString(key))
                {
                    Command = new MethodDelegateCommand(() =>
                    {
                        EmulatorsWorkflowModel.Instance().StartupState = startupStates[lKey];
                    })
                });
            }

            var dialog = (ListDialogModel)ServiceRegistration.Get<IWorkflowManager>().GetModel(Guids.ListDialogModel);
            dialog.ShowDialog("[Emulators.SelectGoodmerge]", items);
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