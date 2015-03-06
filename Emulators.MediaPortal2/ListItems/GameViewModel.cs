using Emulators.ImageHandlers;
using Emulators.MediaPortal2.Models;
using MediaPortal.Common.Commands;
using MediaPortal.Common.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Emulators.MediaPortal2.ListItems
{
    public class GameViewModel : ItemViewModel
    {
        EmulatorsMainModel model;

        public Game Game { get; private set; }

        public GameViewModel(Game game, EmulatorsMainModel model)
        {
            this.model = model;
            Game = game;
            Name = game.Title;
            Description = game.Description;
            using (ThumbGroup thumbs = new ThumbGroup(game))
            {
                FrontCover = thumbs.FrontCoverDefaultPath;
                Fanart = thumbs.FanartDefaultPath;
            }

            Command = new MethodDelegateCommand(() =>
                {
                    model.GameSelected(Game);
                });
        }
    }
}
