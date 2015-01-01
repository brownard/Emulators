using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Emulators
{
    public class ComboBoxItem
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public DBItem Value { get; set; }

        public ComboBoxItem(DBItem item)
        {
            if (item != null)
            {
                Emulator emu = item as Emulator;
                if (emu != null)
                {
                    ID = emu.Id.Value;
                    Name = emu.Title;
                }
                else
                {
                    Game game = (Game)item;
                    ID = game.Id.Value;
                    Name = game.Title;
                }
                Value = item;
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
