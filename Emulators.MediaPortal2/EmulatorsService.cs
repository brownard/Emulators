using Emulators.Import;
using Emulators.MediaPortal2.Messaging;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PluginManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.MediaPortal2
{
    interface IEmulatorsService
    {
        Importer Importer { get; }
    }

    class EmulatorsService : IEmulatorsService, IPluginStateTracker
    {
        Importer importer;

        public Importer Importer
        {
            get { return importer; }
        }

        void Database_OnItemDeleting(DBItem changedItem)
        {
            Game game = changedItem as Game;
            if (game != null && importer != null)
                importer.Remove(game.Id);
        }

        #region IPluginStateTracker

        public void Activated(PluginRuntime pluginRuntime)
        {
            EmulatorsCore.Init(new EmulatorsSettings());
            importer = new Importer();
            EmulatorsCore.Database.OnItemDeleting += Database_OnItemDeleting;
            ServiceRegistration.Set<IEmulatorsService>(this);
            ImporterMessaging.SendImporterMessage(ImporterMessaging.MessageType.Init);
        }

        public void Continue()
        {

        }

        public bool RequestEnd()
        {
            return true;
        }

        public void Shutdown()
        {
            importer.Stop();
            EmulatorsCore.Options.Save();
        }

        public void Stop()
        {

        }

        #endregion
    }
}
