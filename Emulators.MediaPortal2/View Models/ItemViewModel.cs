using MediaPortal.Common.Commands;
using MediaPortal.Common.General;
using MediaPortal.UI.Presentation.DataObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Emulators.MediaPortal2
{
    public class ItemViewModel : ListItem
    {
        protected AbstractProperty _nameProperty;
        protected AbstractProperty _frontCoverProperty;
        protected AbstractProperty _contextCommandProperty;

        public ItemViewModel()
        {
            _nameProperty = new WProperty(typeof(string));
            _frontCoverProperty = new WProperty(typeof(string));
            _contextCommandProperty = new WProperty(typeof(ICommand));
        }

        public AbstractProperty NameProperty { get { return _nameProperty; } }
        public string Name
        {
            get { return (string)_nameProperty.GetValue(); }
            set { _nameProperty.SetValue(value); }
        }

        public AbstractProperty FrontCoverProperty { get { return _frontCoverProperty; } }
        public string FrontCover
        {
            get { return (string)_frontCoverProperty.GetValue(); }
            set { _frontCoverProperty.SetValue(value); }
        }

        public AbstractProperty ContextCommandProperty { get { return _contextCommandProperty; } }
        public ICommand ContextCommand
        {
            get { return (ICommand)_contextCommandProperty.GetValue(); }
            set { _contextCommandProperty.SetValue(value); }
        }
    }
}
