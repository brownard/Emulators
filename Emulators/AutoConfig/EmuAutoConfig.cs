using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using System.Text.RegularExpressions;
using System.Globalization;

namespace Emulators.AutoConfig
{
    public class EmuAutoConfig
    {
        #region Consts

        /// <summary>
        /// Flag used to denote directory where emulator exe is located
        /// </summary>
        public const string USE_EMULATOR_DIRECTORY = "%EMU_EXE_DIR%";

        #endregion

        #region Private Members

        Dictionary<string, EmulatorConfig> autoConfigDictionary;

        #endregion

        #region Ctor/Singleton

        public EmuAutoConfig()
        {
            init();
        }

        static object instanceSync = new object();
        static EmuAutoConfig instance = null;
        public static EmuAutoConfig Instance
        {
            get
            {
                if (instance == null)
                    lock (instanceSync)
                        if (instance == null)
                            instance = new EmuAutoConfig();
                return instance;
            }
        }

        #endregion

        #region Init

        void init()
        {
            autoConfigDictionary = new Dictionary<string, EmulatorConfig>();
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Emulators.AutoConfig.EmuConfigs.xml"));
            }
            catch (Exception ex)
            {
                Logger.LogError("Error loading Emulator auto configuration settings - {0}", ex.Message);
                return;
            }

            foreach (XmlNode platform in doc.SelectNodes("//Platform"))
            {
                XmlNode attr = platform.SelectSingleNode("./@name");
                string platformName;
                if (attr != null && !string.IsNullOrEmpty(attr.Value))
                    platformName = attr.Value;
                else
                    platformName = null;

                attr = platform.SelectSingleNode("./@caseaspect");
                double aspect;
                if (attr == null || !double.TryParse(attr.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out aspect))
                    aspect = 0;

                foreach (XmlNode emulator in platform.SelectNodes("./Emulator"))
                {
                    attr = emulator.SelectSingleNode("./@exe");
                    if (attr != null && !string.IsNullOrEmpty(attr.Value))
                        autoConfigDictionary[wildcardToRegex(attr.Value)] = new EmulatorConfig(emulator, platformName, aspect, attr.Value);
                }
            }
        }

        #endregion

        #region Public Methods

        public EmulatorConfig CheckForSettings(string emuPath)
        {
            if (!string.IsNullOrEmpty(emuPath))
            {
                emuPath = getExeName(emuPath);
                foreach (string key in autoConfigDictionary.Keys)
                    if (Regex.IsMatch(emuPath, key, RegexOptions.IgnoreCase))
                        return autoConfigDictionary[key];
            }
            return null;
        }

        public static void SetupAspectDropdown(System.Windows.Forms.ComboBox thumbAspectComboBox, double aspect)
        {
            bool selected = false;
            string fmtStr = "{0} ({1})";

            thumbAspectComboBox.Items.Clear();
            int index = thumbAspectComboBox.Items.Add(string.Format(fmtStr, 0, "Default"));
            if (aspect == 0)
            {
                thumbAspectComboBox.SelectedIndex = index;
                selected = true;
            }

            index = thumbAspectComboBox.Items.Add(string.Format(fmtStr, 0.71, "DVD"));
            if (aspect == 0.71)
            {
                thumbAspectComboBox.SelectedIndex = index;
                selected = true;
            }

            index = thumbAspectComboBox.Items.Add(string.Format(fmtStr, 1.12, "GameBoy"));
            if (aspect == 1.12)
            {
                thumbAspectComboBox.SelectedIndex = index;
                selected = true;
            }

            index = thumbAspectComboBox.Items.Add(string.Format(fmtStr, 1.14, "CD"));
            if (aspect == 1.14)
            {
                thumbAspectComboBox.SelectedIndex = index;
                selected = true;
            }

            index = thumbAspectComboBox.Items.Add(string.Format(fmtStr, 1.45, "Cartridge"));
            if (aspect == 1.45)
            {
                thumbAspectComboBox.SelectedIndex = index;
                selected = true;
            }

            if (!selected)
            {
                thumbAspectComboBox.Items.Insert(0, string.Format(fmtStr, aspect, "Custom"));
                thumbAspectComboBox.SelectedIndex = 0;
            }
        }

        #endregion

        #region Static Utils

        static string getExeName(string input)
        {
            string ret = input;
            int index = ret.LastIndexOf("\\");
            if (index > -1 && index < ret.Length - 1)
                ret = ret.Substring(index + 1);
            return ret;
        }

        /// <summary>
        /// Converts path wildcards into a Regex pattern
        /// </summary>
        static string wildcardToRegex(string pattern)
        {
            return "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
        }

        #endregion
    }
}
