using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Emulators
{
    interface DBInterface
    {
        string GetDBInsertString();
        string GetDBUpdateString();
        void Save();
        void Delete();
        bool Exists();
    }
}
