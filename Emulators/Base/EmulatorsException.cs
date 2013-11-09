using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Emulators
{
    public class EmulatorsException : Exception
    {
        public EmulatorsException(string format, params object[] args)
        {
            this.message = string.Format(format, args);
        }

        protected string message;
        public override string Message
        {
            get
            {
                return message;
            }
        }
    }

    public class LaunchException : EmulatorsException
    {
        public LaunchException(string format, params object[] args)
            : base(format, args)
        { }
    }

    public class ExtractException : EmulatorsException
    {
        public ExtractException(string format, params object[] args)
            : base(format, args)
        { }
    }

    public class ArchiveException : ExtractException
    {
        public ArchiveException(string format, params object[] args)
            : base(format, args)
        { }
    }

    public class ArchiveEmptyException : ExtractException
    {
        public ArchiveEmptyException(string format, params object[] args)
            : base(format, args)
        { }
    }
}
