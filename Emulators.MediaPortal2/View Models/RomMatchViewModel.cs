using Emulators.Import;
using Emulators.MediaPortal2.Models.Dialogs;
using Emulators.Scrapers;
using MediaPortal.Common;
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

        protected AbstractProperty _statusProperty;
        protected AbstractProperty _nameProperty;
        protected AbstractProperty _currentMatchProperty;
        protected AbstractProperty _contextCommandProperty;

        public RomMatchViewModel(RomMatch romMatch)
        {
            _statusProperty = new WProperty(typeof(RomMatchStatus), RomMatchStatus.PendingMatch);
            _nameProperty = new WProperty(typeof(string), null);
            _currentMatchProperty = new WProperty(typeof(string), null);
            _contextCommandProperty = new WProperty(typeof(ICommand));

            this.romMatch = romMatch;
            Command = new MethodDelegateCommand(commandDelegate);
            ContextCommand = new MethodDelegateCommand(contextCommandDelegate);
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

        public AbstractProperty ContextCommandProperty { get { return _contextCommandProperty; } }
        public ICommand ContextCommand
        {
            get { return (ICommand)_contextCommandProperty.GetValue(); }
            set { _contextCommandProperty.SetValue(value); }
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
                    ServiceRegistration.Get<IEmulatorsService>().Importer.UpdateSelectedMatch(romMatch, match);
                });
                items.Add(item);
            }
            ListDialogModel.Instance().ShowDialog("[Emulators.Dialogs.SelectMatch]", items);
        }

        void contextCommandDelegate()
        {
            Importer importer = ServiceRegistration.Get<IEmulatorsService>().Importer;
            RomMatchStatus status = romMatch.Status;
            ItemsList items = new ItemsList();
            if ((status == RomMatchStatus.Approved || status == RomMatchStatus.NeedsInput) && romMatch.PossibleGameDetails != null && romMatch.PossibleGameDetails.Count > 0)
            {
                items.Add(new ListItem(Consts.KEY_NAME, "[Emulators.Import.Approve]")
                {
                    Command = new MethodDelegateCommand(() => { importer.Approve(romMatch); })
                });
            }

            //items.Add(new ListItem(Consts.KEY_NAME, "[Emulators.Import.ManualSearch]")
            //{
            //    Command = new MethodDelegateCommand(() => { importer.Approve(romMatch); })
            //});

            items.Add(new ListItem(Consts.KEY_NAME, "[Emulators.Import.AddAsBlankGame]")
            {
                Command = new MethodDelegateCommand(() => { importer.Ignore(romMatch); })
            });

            items.Add(new ListItem(Consts.KEY_NAME, "[Emulators.Import.Delete]")
            {
                Command = new MethodDelegateCommand(() => 
                {
                    Game game = romMatch.Game;
                    foreach (GameDisc disc in game.Discs)
                        EmulatorsCore.Options.AddIgnoreFile(disc.Path);
                    game.Delete();
                })
            });

            ListDialogModel.Instance().ShowDialog(romMatch.Filename, items);
        }
    }
}
