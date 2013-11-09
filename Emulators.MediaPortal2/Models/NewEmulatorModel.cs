using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.UI.Presentation.Utilities;
using MediaPortal.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Emulators.MediaPortal2
{
    public class NewEmulatorModel
    {
        PathBrowserCloseWatcher _pathBrowserCloseWatcher = null;

        AbstractProperty _emulatorPathProperty = new WProperty(typeof(string), null);
        public AbstractProperty EmulatorPathProperty { get { return _emulatorPathProperty; } }
        public string EmulatorPath
        {
            get { return (string)_emulatorPathProperty.GetValue(); }
            set { _emulatorPathProperty.SetValue(value); }
        }

        public void ChooseEmulatorPath()
        {
            string emulatorPath = EmulatorPath;
            string initialPath = string.IsNullOrEmpty(emulatorPath) ? null : DosPathHelper.GetDirectory(emulatorPath);
            Guid dialogHandle = ServiceRegistration.Get<IPathBrowser>().ShowPathBrowser("[Emulator.Path]", true, false,
                string.IsNullOrEmpty(initialPath) ? null : LocalFsResourceProviderBase.ToResourcePath(initialPath),
                path =>
                {
                    string choosenPath = LocalFsResourceProviderBase.ToDosPath(path.LastPathSegment.Path);
                    return choosenPath.IsExecutable();
                });
            if (_pathBrowserCloseWatcher != null)
                _pathBrowserCloseWatcher.Dispose();
            _pathBrowserCloseWatcher = new PathBrowserCloseWatcher(this, dialogHandle, chosenPath =>
            {
                EmulatorPath = LocalFsResourceProviderBase.ToDosPath(chosenPath);
            },
                null);
        }
    }
}
