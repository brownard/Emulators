using MediaPortal.UI.Presentation.DataObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.MediaPortal2.Models.Dialogs
{
    public class ListDialogBase
    {
        protected ItemsList items;

        protected virtual void UpdateSelectedFlag() { }

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
