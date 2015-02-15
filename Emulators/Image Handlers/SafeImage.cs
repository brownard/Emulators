using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace Emulators.ImageHandlers
{
    public class SafeImage : IDisposable
    {
        MemoryStream memoryStream;
        Image image;

        public SafeImage(Stream stream) 
        {
            memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
        }

        public SafeImage(string path)
        {
            memoryStream = new MemoryStream(File.ReadAllBytes(path));
        }

        public Image Image
        {
            get
            {
                if (image == null)
                    image = Image.FromStream(memoryStream);
                return image;
            }
        }

        public void Dispose()
        {
            if (image != null)
            {
                image.Dispose();
                image = null;
            }
            if (memoryStream != null)
            {
                memoryStream.Dispose();
                memoryStream = null;
            }
        }
    }
}
