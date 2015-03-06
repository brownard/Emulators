using MediaPortal.Common.Commands;
using MediaPortal.Common.General;
using MediaPortal.UI.Presentation.DataObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.MediaPortal2.ListItems
{
    public class ContextListItem : ListItem
    {
        protected AbstractProperty _contextCommandProperty;

        public ContextListItem()
        {
            _contextCommandProperty = new WProperty(typeof(ICommand));
        }

        public AbstractProperty ContextCommandProperty { get { return _contextCommandProperty; } }
        public ICommand ContextCommand
        {
            get { return (ICommand)_contextCommandProperty.GetValue(); }
            set { _contextCommandProperty.SetValue(value); }
        }
    }
}
