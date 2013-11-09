using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Emulators
{
    public static class ExtensionMethods
    {
        public static void AddRange<T, U>(this List<T> destination, IEnumerable<U> source) where U : T 
        {
            foreach (U item in source)
                destination.Add(item);
        }

        public static bool IsExecutable(this string path)
        {
            return !string.IsNullOrEmpty(path) && (path.EndsWith(".exe") || path.EndsWith(".bat") || path.EndsWith(".cmd"));
        }

        public static bool TryGetExecutablePath(this string pathWithArguments, out string path)
        {
            string arguments;
            return pathWithArguments.TryGetExecutablePath(out path, out arguments);
        }

        public static bool TryGetExecutablePath(this string pathWithArguments, out string path, out string arguments)
        {
            if (pathWithArguments.IsExecutable())
            {
                path = pathWithArguments;
                arguments = "";
                return true;
            }

            Match m = new Regex(@"[.](exe|bat|cmd)\s", RegexOptions.IgnoreCase | RegexOptions.Compiled)
                .Match(pathWithArguments);
            if (m.Success)
            {
                path = pathWithArguments.Remove(m.Index + 4);
                arguments = pathWithArguments.Substring(m.Index + 5);
                return true;
            }
            path = null;
            arguments = null;
            return false;
        }

    }
}
