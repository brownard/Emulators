using MediaPortal.Common.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.MediaPortal2
{
    public class EmulatorViewModel : ItemViewModel
    {
        protected AbstractProperty _nameProperty;
        public AbstractProperty NameProperty { get { return _nameProperty; } }
        public string Name
        {
            get { return (string)_nameProperty.GetValue(); }
            set { _nameProperty.SetValue(value); }
        }

        protected AbstractProperty _frontCoverProperty;
        public AbstractProperty FrontCoverProperty { get { return _frontCoverProperty; } }
        public string FrontCover
        {
            get { return (string)_frontCoverProperty.GetValue(); }
            set { _frontCoverProperty.SetValue(value); }
        }

        public Emulator Emulator { get; private set; }

        public EmulatorViewModel(Emulator emu)
        {
            Emulator = emu;
            _nameProperty = new WProperty(typeof(string), emu.Title);
            using (ThumbGroup thumbs = new ThumbGroup(emu))
                _frontCoverProperty = new WProperty(typeof(string), thumbs.FrontCoverDefaultPath); 
        }
    }
}
