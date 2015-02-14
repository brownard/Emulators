﻿using Emulators.Import;
using MediaPortal.Common;
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

        #region IPluginStateTracker

        public void Activated(PluginRuntime pluginRuntime)
        {
            EmulatorsCore.Init(new EmulatorsSettings());
            importer = new Importer();
            ServiceRegistration.Set<IEmulatorsService>(this);
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