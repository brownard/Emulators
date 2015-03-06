using MediaPortal.Common.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Emulators.MediaPortal2.ListItems
{
    public class ItemViewModel : ContextListItem
    {
        protected AbstractProperty _nameProperty;
        protected AbstractProperty _descriptionProperty;
        protected AbstractProperty _frontCoverProperty;
        protected AbstractProperty _fanartProperty;

        public ItemViewModel()
        {
            _nameProperty = new WProperty(typeof(string));
            _descriptionProperty = new WProperty(typeof(string));
            _frontCoverProperty = new WProperty(typeof(string));
            _fanartProperty = new WProperty(typeof(string));
        }

        public AbstractProperty NameProperty { get { return _nameProperty; } }
        public string Name
        {
            get { return (string)_nameProperty.GetValue(); }
            set { _nameProperty.SetValue(value); }
        }

        public AbstractProperty DescriptionProperty { get { return _descriptionProperty; } }
        public string Description
        {
            get { return (string)_descriptionProperty.GetValue(); }
            set { _descriptionProperty.SetValue(value); }
        }

        public AbstractProperty FrontCoverProperty { get { return _frontCoverProperty; } }
        public string FrontCover
        {
            get { return (string)_frontCoverProperty.GetValue(); }
            set { _frontCoverProperty.SetValue(value); }
        }

        public AbstractProperty FanartProperty { get { return _fanartProperty; } }
        public string Fanart
        {
            get { return (string)_fanartProperty.GetValue(); }
            set { _fanartProperty.SetValue(value); }
        }
    }
}
