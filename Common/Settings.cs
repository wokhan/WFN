using System;
using System.Configuration;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace Wokhan.WindowsFirewallNotifier.Common
{
    public sealed partial class Settings
    {
        public Settings() : base()
        {
            this.SettingsLoaded += Settings_SettingsLoaded;
        }

        private void Settings_SettingsLoaded(object sender, SettingsLoadedEventArgs e)
        {
            ClientSettingsSection woksec = GetGlobalSettings();

            this._enableServiceDetectionGlobalCache = bool.Parse(woksec.Settings.Get("EnableServiceDetectionGlobal").Value.ValueXml.FirstChild.Value);
            this.EnableServiceDetection = _enableServiceDetectionGlobalCache;

            this._useBlockRulesGlobalCache = bool.Parse(woksec.Settings.Get("UseBlockRulesGlobal").Value.ValueXml.FirstChild.Value);
            this.UseBlockRules = _useBlockRulesGlobalCache;
        }

        private static ClientSettingsSection GetGlobalSettings()
        {
            ExeConfigurationFileMap configMap = new ExeConfigurationFileMap();
            configMap.ExeConfigFilename = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WFN.config");
            var cfg = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);
            var setgrp = (ApplicationSettingsGroup)cfg.GetSectionGroup("applicationSettings");
            var woksec = (ClientSettingsSection)setgrp.Sections["Wokhan.WindowsFirewallNotifier.Common.Settings"];

            return woksec;
        }

        public override void Save()
        {
            base.Save();

            if (this._enableServiceDetectionGlobalCache != this.EnableServiceDetection || this._useBlockRulesGlobalCache != this.UseBlockRules)
            {
                var woksec = GetGlobalSettings();

                OverwriteSetting(woksec, "EnableServiceDetectionGlobal", this.EnableServiceDetection.ToString());
                _enableServiceDetectionGlobalCache = this.EnableServiceDetection;
                OverwriteSetting(woksec, "UseBlockRulesGlobal", this.UseBlockRules.ToString());
                _useBlockRulesGlobalCache = this.UseBlockRules;

                woksec.CurrentConfiguration.Save(ConfigurationSaveMode.Modified);

                this.Reload();
            }
        }

        private void OverwriteSetting(ClientSettingsSection section, string settingName, string newValue)
        {
            var val = section.Settings.Get(settingName).Value;
            val.ValueXml.FirstChild.Value = newValue;

            // Forces the setting to be marked as updated as it's done in the ValueXml setter.
            // If not, the setting would be considered as unchanged as we directly changed the underlying value instead above.
            val.ValueXml = val.ValueXml;
        }

        private bool _enableServiceDetection = true;
        private bool _enableServiceDetectionGlobalCache;
        public bool EnableServiceDetection
        {
            get { return _enableServiceDetection; }
            set { _enableServiceDetection = value; }
        }

        private bool _useBlockRules = true;
        private bool _useBlockRulesGlobalCache;
        public bool UseBlockRules
        {
            get { return _useBlockRules; }
            set { _useBlockRules = value; }
        }
    }
}
