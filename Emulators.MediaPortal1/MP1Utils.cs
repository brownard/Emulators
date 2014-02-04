using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Emulators.MediaPortal1
{
    class MP1Utils
    {
        #region Skin Constants

        public const string SKIN_FILE = "Emulators2.xml";
        public const string HOME_HOVER = "hover_Emulators2.png";

        const string DEFAULT_LOGO = "Emulators2_Logo";
        const string DEFAULT_FANART = "Emulators2_Fanart";

        #endregion

        static bool defaultImagesLoaded = false;
        static string defaultLogo;
        public static string DefaultLogo
        {
            get 
            {
                if (!defaultImagesLoaded)
                    initDefaultImages();
                return defaultLogo; 
            }
        }

        static string defaultFanart;
        public static string DefaultFanart
        {
            get
            {
                if (!defaultImagesLoaded)
                    initDefaultImages();
                return defaultFanart;
            }
        }

        static void initDefaultImages()
        {
            string checkLogo = string.Format(@"{0}\Media\{1}", MediaPortal.GUI.Library.GUIGraphicsContext.Skin, DEFAULT_LOGO);
            if (!System.IO.File.Exists(checkLogo + ".png") && System.IO.File.Exists(checkLogo + ".jpg"))
                defaultLogo = checkLogo + ".jpg";
            else
                defaultLogo = checkLogo + ".png";

            string checkFanart = string.Format(@"{0}\Media\{1}", MediaPortal.GUI.Library.GUIGraphicsContext.Skin, DEFAULT_FANART);
            if (!System.IO.File.Exists(checkFanart + ".png") && System.IO.File.Exists(checkFanart + ".jpg"))
                defaultFanart = checkFanart + ".jpg";
            else
                defaultFanart = checkFanart + ".png";

            defaultImagesLoaded = true;
        }


        public static List<StartupStateHandler> GetStartupOptions(out int selectedValue)
        {
            selectedValue = Options.Instance.GetIntOption("startupstate");
            if (selectedValue < -1 || selectedValue > 3)
                selectedValue = -1;

            List<StartupStateHandler> opts = new List<StartupStateHandler>();
            opts.Add(new StartupStateHandler() { Name = Translator.Instance.lastused, Value = -1 });
            opts.Add(new StartupStateHandler() { Name = StartupState.EMULATORS.Translate(), Value = (int)StartupState.EMULATORS });
            opts.Add(new StartupStateHandler() { Name = StartupState.GROUPS.Translate(), Value = (int)StartupState.GROUPS });
            opts.Add(new StartupStateHandler() { Name = StartupState.PCGAMES.Translate(), Value = (int)StartupState.PCGAMES });
            opts.Add(new StartupStateHandler() { Name = StartupState.FAVOURITES.Translate(), Value = (int)StartupState.FAVOURITES });
            return opts;
        }

        public static OpenFileDialog OpenFileDialog(string title, string filter, string initialDirectory)
        {
            OpenFileDialog dlg = new OpenFileDialog();

            dlg.Title = title;
            dlg.Filter = filter;
            dlg.InitialDirectory = initialDirectory;

            dlg.CheckFileExists = true;
            dlg.CheckPathExists = true;
            dlg.ValidateNames = true;

            dlg.Multiselect = false;
            dlg.RestoreDirectory = true;

            return dlg;
        }

        public static FolderBrowserDialog OpenFolderDialog(string title, string initialDirectory)
        {
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            dlg.Description = title;
            dlg.SelectedPath = initialDirectory;
            return dlg;
        }

        public static int GetComboBoxDropDownWidth(ComboBox comboBox)
        {
            int maxWidth = 0, temp = 0;

            foreach (var obj in comboBox.Items)
            {
                temp = TextRenderer.MeasureText(obj.ToString(), comboBox.Font).Width;
                if (temp > maxWidth)
                {
                    maxWidth = temp;
                }
            }
            if (comboBox.Items.Count > comboBox.MaxDropDownItems)
                maxWidth += SystemInformation.VerticalScrollBarWidth;
            return maxWidth;
        }

        public static void ShowMPDialog(string message, params object[] args)
        {
            message = string.Format(message, args);
            string heading = Options.Instance.GetStringOption("shownname");

            if (EmulatorsSettings.Instance.IsConfig)
            {
                MessageBox.Show(message, heading, MessageBoxButtons.OK);
                return;
            }

            MediaPortal.Dialogs.GUIDialogOK dlg_error = (MediaPortal.Dialogs.GUIDialogOK)MediaPortal.GUI.Library.GUIWindowManager.GetWindow
                ((int)MediaPortal.GUI.Library.GUIWindow.Window.WINDOW_DIALOG_OK);
            if (dlg_error != null)
            {
                dlg_error.Reset();
                dlg_error.SetHeading(heading);

                string[] lines = message.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                for (int x = 0; x < lines.Length; x++)
                    dlg_error.SetLine(x + 1, lines[x]);
                dlg_error.DoModal(MediaPortal.GUI.Library.GUIWindowManager.ActiveWindow);
            }
        }

        public static void ShowProgressDialog(IBackgroundTask handler)
        {
            if (EmulatorsSettings.Instance.IsConfig)
            {
                using (Conf_ProgressDialog dialog = new Conf_ProgressDialog(handler))
                    dialog.ShowDialog();
            }
            else
            {
                GUIProgressDialogHandler guiDlg = new GUIProgressDialogHandler(handler);
                guiDlg.ShowDialog();
            }
        }
    }
}
