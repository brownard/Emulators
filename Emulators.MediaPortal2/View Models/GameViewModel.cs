using MediaPortal.Common.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Emulators.MediaPortal2
{
    public class GameViewModel : ItemViewModel
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

        public Game Game { get; private set; }

        public GameViewModel(Game game)
        {
            Game = game;
            _nameProperty = new WProperty(typeof(string), game.Title);
            using (ThumbGroup thumbs = new ThumbGroup(game))
                _frontCoverProperty = new WProperty(typeof(string), thumbs.FrontCoverDefaultPath);
        }
    }
}
