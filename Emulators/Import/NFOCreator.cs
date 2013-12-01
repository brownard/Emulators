using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace Emulators.Import
{
    class NFOCreator
    {
        public static void CreateLocalInfo(Game game)
        {
            List<string> thumbPaths = new List<string>();
            using (ThumbGroup thumbGroup = new ThumbGroup(game))
            {
                string thumbPath;
                if (thumbGroup.HasLocalThumb(ThumbType.FrontCover, out thumbPath))
                    thumbPaths.Add(thumbPath);
                if (thumbGroup.HasLocalThumb(ThumbType.BackCover, out thumbPath))
                    thumbPaths.Add(thumbPath);
                if (thumbGroup.HasLocalThumb(ThumbType.TitleScreen, out thumbPath))
                    thumbPaths.Add(thumbPath);
                if (thumbGroup.HasLocalThumb(ThumbType.InGameScreen, out thumbPath))
                    thumbPaths.Add(thumbPath);
                if (thumbGroup.HasLocalThumb(ThumbType.Fanart, out thumbPath))
                    thumbPaths.Add(thumbPath);
            }

            XmlDocument nfo = null;
            foreach (GameDisc disc in game.Discs)
            {
                FileInfo file = new FileInfo(disc.Path);
                if (file.Exists)
                {
                    string prefix = Path.Combine(file.DirectoryName, Path.GetFileNameWithoutExtension(file.Name));
                    string nfoPath = prefix + ".nfo";
                    if (!File.Exists(nfoPath))
                    {
                        if (nfo == null)
                            nfo = createNFO(game);
                        nfo.Save(nfoPath);
                    }

                    foreach (string thumbPath in thumbPaths)
                        copyThumb(thumbPath, prefix);
                }
            }
        }

        static void copyThumb(string thumbPath, string destinationPrefix)
        {
            string thumbName = destinationPrefix + "_" + Path.GetFileNameWithoutExtension(thumbPath);
            if (!File.Exists(thumbName + ".png") && !File.Exists(thumbName + ".jpg"))
            {
                try { File.Copy(thumbPath, thumbName + Path.GetExtension(thumbPath)); }
                catch (Exception ex)
                {
                    Logger.LogWarn("Failed to copy thumb '{0}' to '{1}' - {2}, {3}", thumbPath, thumbName + Path.GetExtension(thumbPath), ex, ex.Message);
                }
            }
        }

        static XmlDocument createNFO(Game game)
        {
            System.Globalization.CultureInfo culture = System.Globalization.CultureInfo.InvariantCulture;
            XmlDocument nfo = new XmlDocument();
            nfo.AppendChild(nfo.CreateXmlDeclaration("1.0", "UTF-8", null));
            XmlElement gameNode = nfo.CreateElement("Game");
            nfo.AppendChild(gameNode);

            XmlElement detailsNode = nfo.CreateElement("Title");
            detailsNode.InnerText = game.Title;
            gameNode.AppendChild(detailsNode);

            detailsNode = nfo.CreateElement("Developer");
            detailsNode.InnerText = game.Developer;
            gameNode.AppendChild(detailsNode);

            detailsNode = nfo.CreateElement("Year");
            detailsNode.InnerText = game.Year.ToString(culture);
            gameNode.AppendChild(detailsNode);

            detailsNode = nfo.CreateElement("Genre");
            detailsNode.InnerText = game.Genre;
            gameNode.AppendChild(detailsNode);

            detailsNode = nfo.CreateElement("Description");
            detailsNode.InnerText = game.Description;
            gameNode.AppendChild(detailsNode);

            detailsNode = nfo.CreateElement("Grade");
            detailsNode.InnerText = game.Grade.ToString(culture);
            gameNode.AppendChild(detailsNode);

            return nfo;
        }
    }
}
