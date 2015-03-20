using MediaPortal.Common;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.UI.Presentation.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.MediaPortal2.PathBrowser
{
    class PathBrowserHandler : IDisposable
    {
        PathBrowserCloseWatcher _pathBrowserCloseWatcher;

        public void ShowPathSelect(string displayLabel, string initialPath, bool fileSelect, Func<string, bool> validate, Action<string> completed)
        {
            initialPath = string.IsNullOrEmpty(initialPath) ? null : DosPathHelper.GetDirectory(initialPath);
            Guid dialogHandle = ServiceRegistration.Get<IPathBrowser>().ShowPathBrowser(displayLabel, fileSelect, false,
                string.IsNullOrEmpty(initialPath) ? null : LocalFsResourceProviderBase.ToResourcePath(initialPath),
                path =>
                {
                    string choosenPath = LocalFsResourceProviderBase.ToDosPath(path.LastPathSegment.Path);
                    return validate(choosenPath);
                });

            if (_pathBrowserCloseWatcher != null)
                _pathBrowserCloseWatcher.Dispose();
            _pathBrowserCloseWatcher = new PathBrowserCloseWatcher(this, dialogHandle,
                chosenPath => completed(LocalFsResourceProviderBase.ToDosPath(chosenPath)),
                null);
        }

        public void Dispose()
        {
            if (_pathBrowserCloseWatcher != null)
            {
                _pathBrowserCloseWatcher.Dispose();
                _pathBrowserCloseWatcher = null;
            }
        }
    }
}
