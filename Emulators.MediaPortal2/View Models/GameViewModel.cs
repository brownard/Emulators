using Emulators.ImageHandlers;
using MediaPortal.Common.Commands;
using MediaPortal.Common.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Emulators.MediaPortal2
{
    public class GameViewModel : ItemViewModel
    {
        EmulatorsWorkflowModel model;

        public Game Game { get; private set; }

        public GameViewModel(Game game, EmulatorsWorkflowModel model)
        {
            this.model = model;
            Game = game;
            Name = game.Title;
            using (ThumbGroup thumbs = new ThumbGroup(game))
                FrontCover = thumbs.FrontCoverDefaultPath;

            Command = new MethodDelegateCommand(() =>
                {
                    model.GameSelected(Game);
                });
        }
    }
}
