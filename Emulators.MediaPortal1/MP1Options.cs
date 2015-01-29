using MediaPortal.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Emulators.MediaPortal1
{
    class MP1Options : Options
    {
        public MP1Options()
        {
            SavePath = Config.GetFile(Config.Dir.Config, "Emulators_2.xml");
        }

        /// <summary>
        /// The display name of the plugin
        /// </summary>
        [OptionAttribute(Default = "Emulators")]
        public string PluginDisplayName { get; set; }
        /// <summary>
        /// The language file to use
        /// </summary>
        [OptionAttribute(Default = "English")]
        public string Language { get; set; }
        public T ReadOption<T>(Func<MP1Options, T> reader)
        {
            EnterReadLock();
            T value = reader(this);
            ExitReadLock();
            return value;
        }
    }
}
