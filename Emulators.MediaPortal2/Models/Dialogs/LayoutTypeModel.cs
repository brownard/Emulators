using MediaPortal.Common.Commands;
using MediaPortal.UI.Presentation.DataObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.MediaPortal2.Models.Dialogs
{
    public class LayoutTypeModel : ListDialogBase
    {
        const string KEY_LAYOUT_TYPE = "LayoutType";

        public LayoutTypeModel()
        {
            items = new ItemsList();

            ListItem listItem = new ListItem(Consts.KEY_NAME, "List")
            {
                Command = new MethodDelegateCommand(() => UpdateLayout(LayoutType.List))
            };
            listItem.AdditionalProperties[KEY_LAYOUT_TYPE] = LayoutType.List;
            items.Add(listItem);

            ListItem gridItem = new ListItem(Consts.KEY_NAME, "Grid")
            {
                Command = new MethodDelegateCommand(() => UpdateLayout(LayoutType.Icons))
            };
            gridItem.AdditionalProperties[KEY_LAYOUT_TYPE] = LayoutType.Icons;
            items.Add(gridItem);

            ListItem coversItem = new ListItem(Consts.KEY_NAME, "Covers")
            {
                Command = new MethodDelegateCommand(() => UpdateLayout(LayoutType.Cover))
            };
            coversItem.AdditionalProperties[KEY_LAYOUT_TYPE] = LayoutType.Cover;
            items.Add(coversItem);
        }

        protected void UpdateLayout(LayoutType layoutType)
        {
            var model = EmulatorsMainModel.Instance();
            model.LayoutType = layoutType;
        }

        protected override void UpdateSelectedFlag()
        {
            var model = EmulatorsMainModel.Instance();
            LayoutType currentLayout = model.LayoutType;
            foreach (ListItem item in items)
            {
                object layout;
                if (item.AdditionalProperties.TryGetValue(KEY_LAYOUT_TYPE, out layout))
                    item.Selected = (LayoutType)layout == currentLayout;
            }
        }
    }
}
