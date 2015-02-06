using MediaPortal.Common.General;
using MediaPortal.UI.Presentation.DataObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.MediaPortal2
{
    public class RomMatchViewModel : ListItem
    {
        RomMatch romMatch;

        AbstractProperty _statusProperty;
        AbstractProperty _nameProperty;
        AbstractProperty _currentMatchProperty;

        public RomMatchViewModel(RomMatch romMatch)
        {
            _statusProperty = new WProperty(typeof(string), null);
            _nameProperty = new WProperty(typeof(string), null);
            _currentMatchProperty = new WProperty(typeof(string), null);

            this.romMatch = romMatch;
            Update();
        }
        public RomMatchViewModel()
        {
            _statusProperty = new WProperty(typeof(string), null);
            _nameProperty = new WProperty(typeof(string), null);
            _currentMatchProperty = new WProperty(typeof(string), null);

            Name = "testName";
            Status = "testStatus";
            CurrentMatch = "testMatch";
        }

        public void Update()
        {
            Name = romMatch.Filename;
            Status = romMatch.Status.ToString();
            CurrentMatch = romMatch.GameDetails == null ? null : romMatch.GameDetails.Title;
        }

        public AbstractProperty StatusProperty { get { return _statusProperty; } }
        public string Status
        {
            get { return (string)_statusProperty.GetValue(); }
            set { _statusProperty.SetValue(value); }
        }

        public AbstractProperty NameProperty { get { return _nameProperty; } }
        public string Name
        {
            get { return (string)_nameProperty.GetValue(); }
            set { _nameProperty.SetValue(value); }
        }

        public AbstractProperty CurrentMatchProperty { get { return _currentMatchProperty; } }
        public string CurrentMatch
        {
            get { return (string)_currentMatchProperty.GetValue(); }
            set { _currentMatchProperty.SetValue(value); }
        }
    }
}
