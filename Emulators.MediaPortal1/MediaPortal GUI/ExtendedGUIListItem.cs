using System;
using System.Collections.Generic;
using System.Text;
using MediaPortal.GUI.Library;
using Emulators.MediaPortal1;
using Emulators.ImageHandlers;

namespace Emulators
{
    public class ExtendedGUIListItem: GUIListItem
    {
        public ExtendedGUIListItem(DBItem item)
        {
            Game game = item as Game;
            if (game != null)
            {
                Sortable = true;
                UpdateGameInfo(game);
                return;
            }

            Emulator emu = item as Emulator;
            if (emu != null)
            {
                associatedEmulator = emu;
                Label = emu.Title;
                thumbGroup = new ThumbGroup(emu);
                ThumbnailImage = thumbGroup.FrontCoverDefaultPath;
                if (string.IsNullOrEmpty(ThumbnailImage))
                    ThumbnailImage = MP1Utils.DefaultLogo;
                videoPreview = emu.VideoPreview;
                if (!string.IsNullOrEmpty(videoPreview))
                    VideoPreviewId = "emu" + emu.Id.ToString();
                return;
            }

            RomGroup group = item as RomGroup;
            if (group != null)
            {
                romGroup = group;
                Label = group.Title;
                IsGroup = true;
                IsFavourites = group.Favourite;
                if (group.ThumbGroup != null)
                {
                    thumbGroup = group.ThumbGroup;
                    ThumbnailImage = thumbGroup.FrontCoverDefaultPath;
                }
                if (string.IsNullOrEmpty(ThumbnailImage))
                    ThumbnailImage = MP1Utils.DefaultLogo;
            }
        }

        public void UpdateGameInfo(Game game)
        {
            lock (game.SyncRoot)
            {
                if (game.Id == null)
                    return;

                associatedGame = game;
                Label = game.Title;
                thumbGroup = new ThumbGroup(game);
                ThumbnailImage = thumbGroup.FrontCoverDefaultPath;
                if (string.IsNullOrEmpty(ThumbnailImage))
                    ThumbnailImage = MP1Utils.DefaultLogo;
                ReleaseYear = game.Year;
                PlayCount = game.PlayCount;
                LastPlayed = game.Latestplay;
                Company = game.Developer;
                Grade = game.Grade;
                videoPreview = game.VideoPreview;
                if (string.IsNullOrEmpty(videoPreview) && EmulatorsCore.Options.ReadOption(o => o.FallBackToEmulatorVideo))
                    videoPreview = game.ParentEmulator.VideoPreview;
                if (!string.IsNullOrEmpty(videoPreview))
                    VideoPreviewId = "game" + game.Id.ToString();
            }
        }

        Emulator associatedEmulator = null;
        public Emulator AssociatedEmulator
        {
            get { return associatedEmulator; }
        }

        Game associatedGame = null;
        public Game AssociatedGame
        {
            get { return associatedGame; }
        }

        RomGroup romGroup = null;
        public RomGroup RomGroup
        {
            get { return romGroup; }
        }
        public bool IsGroup { get; set; }
        bool isFavourites = false;
        public bool IsFavourites
        {
            get { return isFavourites; }
            set { isFavourites = value; }
        }

        ExtendedGUIListItem parent = null;
        public ExtendedGUIListItem Parent
        {
            get { return parent; }
            set { parent = value; }
        }

        int parentIndex = 0;
        public int ParentIndex
        {
            get { return parentIndex; }
            set { parentIndex = value; }
        }

        object syncRoot = new object();
        public object SyncRoot { get { return syncRoot; } }

        ThumbGroup thumbGroup = null;
        public ThumbGroup ThumbGroup
        {
            get
            {
                if (thumbGroup == null)
                {
                    if (associatedEmulator != null)
                        thumbGroup = new ThumbGroup(associatedEmulator);
                    else if (associatedGame != null)
                        thumbGroup = new ThumbGroup(associatedGame);
                    else if (RomGroup != null)
                        thumbGroup = RomGroup.ThumbGroup;
                }
                return thumbGroup;
            }
        }        

        string videoPreview = null;
        public string VideoPreview
        {
            get 
            {
                return videoPreview;
            }
        }
        public string VideoPreviewId { get; private set; }

        public bool Sortable { get; protected set; }

        public int ReleaseYear { get; set; }
        public int PlayCount { get; set; }
        public DateTime LastPlayed { get; set; }
        public string Company { get; set; }
        public int Grade { get; set; }

        public void SetLabel2(ListItemProperty property)
        {
            if (associatedGame == null)
                return;

            switch (property)
            {
                case ListItemProperty.COMPANY:
                    Label2 = associatedGame.Developer;
                    break;
                case ListItemProperty.GRADE:
                    Label2 = associatedGame.Grade.ToString();
                    break;
                case ListItemProperty.LASTPLAYED:
                    if (associatedGame.Latestplay != DateTime.MinValue)
                        Label2 = associatedGame.Latestplay.ToShortDateString();
                    else
                        Label2 = Translator.Instance.never;
                    break;
                case ListItemProperty.PLAYCOUNT:
                    Label2 = associatedGame.PlayCount.ToString();
                    break;
                case ListItemProperty.YEAR:
                    if (ReleaseYear > 0)
                        Label2 = ReleaseYear.ToString();
                    else
                        Label2 = "";
                    break;
                default:
                    Label2 = "";
                    break;
            }
        }

        static Dictionary<string, string> getProps()
        {
            Dictionary<string, string> guiProperties = new Dictionary<string, string>();
            guiProperties.Add("#Emulators2.CurrentItem.isemulator", "no");
            guiProperties.Add("#Emulators2.CurrentItem.isgame", "no");

            guiProperties.Add("#Emulators2.CurrentItem.title", "");
            guiProperties.Add("#Emulators2.CurrentItem.emulatortitle", "");
            guiProperties.Add("#Emulators2.CurrentItem.coverflowlabel", "");
            guiProperties.Add("#Emulators2.CurrentItem.description", "");
            guiProperties.Add("#Emulators2.CurrentItem.year", "");
            guiProperties.Add("#Emulators2.CurrentItem.genre", "");
            guiProperties.Add("#Emulators2.CurrentItem.company", "");
            guiProperties.Add("#Emulators2.CurrentItem.latestplaydate", "");
            guiProperties.Add("#Emulators2.CurrentItem.latestplaytime", "");
            guiProperties.Add("#Emulators2.CurrentItem.playcount", "");
            guiProperties.Add("#Emulators2.CurrentItem.grade", "");
            guiProperties.Add("#Emulators2.CurrentItem.caseaspect", "0");

            guiProperties.Add("#Emulators2.CurrentItem.goodmerge", "no");
            guiProperties.Add("#Emulators2.CurrentItem.favourite", "no");
            guiProperties.Add("#Emulators2.CurrentItem.isgoodmerge", "False");
            guiProperties.Add("#Emulators2.CurrentItem.isfavourite", "False");

            guiProperties.Add("#Emulators2.CurrentItem.currentdisc", "0");
            guiProperties.Add("#Emulators2.CurrentItem.totaldiscs", "0");

            guiProperties.Add("#Emulators2.CurrentItem.path", "");
            guiProperties.Add("#Emulators2.CurrentItem.selectedgoodmerge", "");

            guiProperties.Add("#Emulators2.CurrentItem.Profile.title", "");
            guiProperties.Add("#Emulators2.CurrentItem.Profile.emulatorpath", "");
            guiProperties.Add("#Emulators2.CurrentItem.Profile.arguments", "");
            guiProperties.Add("#Emulators2.CurrentItem.Profile.workingdirectory", "");
            guiProperties.Add("#Emulators2.CurrentItem.Profile.suspendmp", "False");
            guiProperties.Add("#Emulators2.CurrentItem.Profile.usequotes", "False");
            guiProperties.Add("#Emulators2.CurrentItem.Profile.mountimages", "False");
            guiProperties.Add("#Emulators2.CurrentItem.Profile.escapetoexit", "False");
            guiProperties.Add("#Emulators2.CurrentItem.Profile.checkcontroller", "False");
            guiProperties.Add("#Emulators2.CurrentItem.Profile.launchedexe", "");

            return guiProperties;
        }

        public static void ClearGUIProperties()
        {
            foreach (KeyValuePair<string, string> prop in getProps())
                GUIPropertyManager.SetProperty(prop.Key, prop.Value);
        }

        public void SetGUIProperties()
        {
            Dictionary<string, string> guiProperties = getProps();
            Emulator emu = associatedEmulator;
            Game game = associatedGame;
            
            if (isFavourites || IsGroup)
            {
                guiProperties["#Emulators2.CurrentItem.title"] = Label;
            }
            else if (emu != null)
            {
                guiProperties["#Emulators2.CurrentItem.isemulator"] = "yes";
                guiProperties["#Emulators2.CurrentItem.isgame"] = "no";

                guiProperties["#Emulators2.CurrentItem.title"] = emu.Title;
                guiProperties["#Emulators2.CurrentItem.grade"] = emu.Grade.ToString();
                guiProperties["#Emulators2.CurrentItem.description"] = emu.Description;
                guiProperties["#Emulators2.CurrentItem.company"] = emu.Developer;
                guiProperties["#Emulators2.CurrentItem.year"] = emu.Year > 0 ? emu.Year.ToString() : "";
                guiProperties["#Emulators2.CurrentItem.caseaspect"] = emu.CaseAspect.ToString(System.Globalization.CultureInfo.InvariantCulture);
                //guiProperties["#Emulators2.CurrentItem.isarcade"] = emu.IsArcade.ToString();
            }
            else if (game != null)
            {
                guiProperties["#Emulators2.CurrentItem.isemulator"] = "no";
                guiProperties["#Emulators2.CurrentItem.isgame"] = "yes";
                guiProperties["#Emulators2.CurrentItem.goodmerge"] = game.IsGoodmerge ? "yes" : "no";
                guiProperties["#Emulators2.CurrentItem.isgoodmerge"] = game.IsGoodmerge.ToString();
                guiProperties["#Emulators2.CurrentItem.title"] = game.Title;

                guiProperties["#Emulators2.CurrentItem.coverflowlabel"] = string.IsNullOrEmpty(Label2) ? game.ParentEmulator.ToString() : Label2;
                guiProperties["#Emulators2.CurrentItem.emulatortitle"] = game.ParentEmulator.Title;
                guiProperties["#Emulators2.CurrentItem.grade"] = game.Grade.ToString();
                guiProperties["#Emulators2.CurrentItem.description"] = game.Description;
                guiProperties["#Emulators2.CurrentItem.year"] = game.Year > 0 ? game.Year.ToString() : "";
                guiProperties["#Emulators2.CurrentItem.genre"] = game.Genre;
                guiProperties["#Emulators2.CurrentItem.company"] = game.Developer;
                guiProperties["#Emulators2.CurrentItem.caseaspect"] = game.ParentEmulator.CaseAspect.ToString(System.Globalization.CultureInfo.InvariantCulture);
                //guiProperties["#Emulators2.CurrentItem.isarcade"] = game.ParentEmulator.IsArcade.ToString();

                if (game.Latestplay.CompareTo(DateTime.MinValue) == 0)
                {
                    guiProperties["#Emulators2.CurrentItem.latestplaydate"] = Translator.Instance.never;
                    guiProperties["#Emulators2.CurrentItem.latestplaytime"] = "";
                }
                else
                {
                    guiProperties["#Emulators2.CurrentItem.latestplaydate"] = game.Latestplay.ToShortDateString();
                    guiProperties["#Emulators2.CurrentItem.latestplaytime"] = game.Latestplay.ToShortTimeString();
                }

                guiProperties["#Emulators2.CurrentItem.playcount"] = game.PlayCount.ToString();
                guiProperties["#Emulators2.CurrentItem.favourite"] = game.Favourite ? "yes" : "no";
                guiProperties["#Emulators2.CurrentItem.isfavourite"] = game.Favourite.ToString();

                if (game.CurrentDisc != null)
                {
                    guiProperties["#Emulators2.CurrentItem.currentdisc"] = game.CurrentDisc.Number.ToString();
                    guiProperties["#Emulators2.CurrentItem.path"] = game.CurrentDisc.Path;
                    guiProperties["#Emulators2.CurrentItem.selectedgoodmerge"] = game.CurrentDisc.LaunchFile;
                }
                guiProperties["#Emulators2.CurrentItem.totaldiscs"] = game.Discs.Count.ToString();

                if (game.CurrentProfile != null)
                {
                    guiProperties["#Emulators2.CurrentItem.Profile.title"] = game.CurrentProfile.Title;
                    guiProperties["#Emulators2.CurrentItem.Profile.emulatorpath"] = game.CurrentProfile.EmulatorPath;
                    guiProperties["#Emulators2.CurrentItem.Profile.arguments"] = game.CurrentProfile.Arguments;
                    guiProperties["#Emulators2.CurrentItem.Profile.workingdirectory"] = game.CurrentProfile.WorkingDirectory;
                    guiProperties["#Emulators2.CurrentItem.Profile.suspendmp"] = game.CurrentProfile.SuspendMP.ToString();
                    guiProperties["#Emulators2.CurrentItem.Profile.usequotes"] = game.CurrentProfile.UseQuotes.ToString();
                    guiProperties["#Emulators2.CurrentItem.Profile.mountimages"] = game.CurrentProfile.MountImages.ToString();
                    guiProperties["#Emulators2.CurrentItem.Profile.escapetoexit"] = game.CurrentProfile.EscapeToExit.ToString();
                    guiProperties["#Emulators2.CurrentItem.Profile.checkcontroller"] = game.CurrentProfile.CheckController.ToString();
                    guiProperties["#Emulators2.CurrentItem.Profile.launchedexe"] = game.CurrentProfile.LaunchedExe;
                }
            }

            foreach (KeyValuePair<string, string> prop in guiProperties)
                GUIPropertyManager.SetProperty(prop.Key, prop.Value);
        }
        
        public int ListPosition { get; set; }
    }

    
}
