using Emulators.Launcher;
using MediaPortal.Common.Commands;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Emulators.MediaPortal2
{
    public class GoodmergeSelectModel : BaseDialogModel
    {
        ItemsList _menuItems;
        public ItemsList MenuItems { get { return _menuItems; } }

        public GoodmergeSelectModel()
        {
            _menuItems = new ItemsList();
        }

        public void UpdateItems(GameViewModel selectedGame)
        {
            _menuItems = new ItemsList();
            Game game = selectedGame.Game;
            List<string> files = SharpCompressExtractor.ViewFiles(game.CurrentDisc.Path);
            int matchIndex = GoodmergeHandler.GetFileIndex(game.CurrentDisc.LaunchFile, files, game.CurrentProfile.GetGoodmergeTags());
            if (files != null)
            {
                for (int x = 0; x < files.Count; x++)
                {
                    string file = files[x];
                    _menuItems.Add(new ListItem(Consts.KEY_NAME, file)
                    {
                        Selected = x == matchIndex,
                        Command = new MethodDelegateCommand(() =>
                        {
                            game.CurrentDisc.LaunchFile = file;
                            game.CurrentDisc.Commit();
                        })
                    });
                }
            }
        }

        public override Guid DialogGuid
        {
            get { return Guids.GoodmergeDialogWorkflow; }
        }
    }
}
