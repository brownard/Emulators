using Emulators.Scrapers;
using MediaPortal.Common.Commands;
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
            _statusProperty = new WProperty(typeof(RomMatchStatus), RomMatchStatus.PendingMatch);
            _nameProperty = new WProperty(typeof(string), null);
            _currentMatchProperty = new WProperty(typeof(string), null);

            this.romMatch = romMatch;
            Command = new MethodDelegateCommand(commandDelegate);
            Update();
        }

        public void Update()
        {
            Name = romMatch.Filename;
            Status = romMatch.Status;
            CurrentMatch = romMatch.GameDetails == null ? null : romMatch.GameDetails.ToString();
        }

        public RomMatch RomMatch
        {
            get { return romMatch; }
        }

        public AbstractProperty StatusProperty { get { return _statusProperty; } }
        public RomMatchStatus Status
        {
            get { return (RomMatchStatus)_statusProperty.GetValue(); }
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

        void commandDelegate()
        {
            var matches = romMatch.PossibleGameDetails;
            if (matches == null || matches.Count == 0)
                return;

            ScraperResult currentMatch = romMatch.GameDetails;
            ItemsList items = new ItemsList();
            for (int i = 0; i < matches.Count; i++)
            {
                ScraperResult match = matches[i];
                ListItem item = new ListItem(Consts.KEY_NAME, match.ToString());
                item.Command = new MethodDelegateCommand(() =>
                {
                    EmulatorsWorkflowModel.Instance().Importer.UpdateSelectedMatch(romMatch, match);
                });
                items.Add(item);
            }
            ListDialogModel.Instance().ShowDialog("[Emulators.Dialogs.SelectMatch]", items);
        }
    }
}
