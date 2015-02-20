using MediaPortal.Common;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Emulators.MediaPortal2.Models.Dialogs
{
    public class ListDialogModel
    {
        public string Header { get; private set; }
        public ItemsList Items { get; private set; }

        public static ListDialogModel Instance()
        {
            return (ListDialogModel)ServiceRegistration.Get<IWorkflowManager>().GetModel(Guids.ListDialogModel);
        }

        public void ShowDialog(string header, ItemsList items)
        {
            Header = header;
            Items = items;

            IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
            screenManager.ShowDialog(Consts.DIALOG_LIST);
        }
    }
}
