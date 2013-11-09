﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentNHibernate.Mapping;

namespace CommunityServerWindowsService
{
    public class ImageTitleScreenMatchMap : ClassMap<ImageTitleScreenMatch>
    {
        public ImageTitleScreenMatchMap()
        {
            Id(x => x.id);
            References(x => x.game);
            References(x => x.ImageTitleScreen);
            Map(x => x.count);
        }
    }
}
