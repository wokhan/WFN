using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Security.Policy;

namespace Wokhan.WindowsFirewallNotifier.Common.Helpers
{
    public class CustomSettingsProvider : SettingsProvider, IApplicationSettingsProvider
    {
        private Dictionary<string, object> _valuesCache = new Dictionary<string, object>();

        private static string SectionName = "Wokhan.WindowsFirewallNotifier.Configuration";
        public static string UserConfigurationPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Wokhan Solutions", "WFN", "user.config");

        public static string SharedConfigurationPath
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WFN.config"); }
            set { }
        }

        public override string ApplicationName
        {
            get { return "WFNPotato"; } //FIXME: Erm...?
            set { }
        }

        public override SettingsPropertyValueCollection GetPropertyValues(SettingsContext context, SettingsPropertyCollection collection)
        {
            _valuesCache.Clear();

            var r = new SettingsPropertyValueCollection();// base.GetPropertyValues(context, collection);

            var cfg = GetSharedConfiguration();
            if (!cfg.HasFile)
            {
                throw new ApplicationException("Configuration file missing!");
            }

            ClientSettingsSection appSettings = GetApplicationSettingsSection(cfg);
            var sets = collection.Cast<SettingsProperty>().Where(x => !IsUserSetting(x));
            ExtractSettings(sets, r, appSettings);

            ClientSettingsSection userInitialSettings = GetUserSettingsSection(cfg);
            sets = collection.Cast<SettingsProperty>().Where(x => IsUserSetting(x));
            ExtractSettings(sets, r, userInitialSettings);

            ClientSettingsSection userSettings = GetUserSettings();
            if (userSettings != null)
            {
                sets = collection.Cast<SettingsProperty>().Where(x => IsUserSetting(x));
                ExtractSettings(sets, r, userSettings);
            }

            return r;
        }

        private void ExtractSettings(IEnumerable<SettingsProperty> sets, SettingsPropertyValueCollection r, ClientSettingsSection userInitialSettings)
        {
            foreach (var s in sets)
            {
                var value = userInitialSettings.Settings.Get(s.Name).Value.ValueXml.FirstChild.Value;

                _valuesCache[s.Name] = value;

                r.Remove(s.Name);
                r.Add(new SettingsPropertyValue(new SettingsProperty(s)) { IsDirty = false, SerializedValue = value });
            }
        }

        private static Configuration GetSharedConfiguration()
        {
            ExeConfigurationFileMap configMap = new ExeConfigurationFileMap();
            configMap.ExeConfigFilename = SharedConfigurationPath;
            return ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);
        }

        private static ClientSettingsSection GetUserSettingsSection(Configuration cfg)
        {
            return (ClientSettingsSection)cfg.GetSectionGroup("userSettings").Sections[SectionName];
        }

        private static ClientSettingsSection GetApplicationSettingsSection(Configuration cfg)
        {
            return (ClientSettingsSection)cfg.GetSectionGroup("applicationSettings").Sections[SectionName];
        }

        private static ClientSettingsSection GetUserSettings(bool createIfNone = false)
        {
            if (!File.Exists(UserConfigurationPath))
            {
                if (createIfNone)
                {
                    var sharedcfg = GetSharedConfiguration();
                    sharedcfg.SectionGroups.Remove("applicationSettings");
                    sharedcfg.SaveAs(UserConfigurationPath, ConfigurationSaveMode.Minimal);
                }
                else
                {
                    return null;
                }
            }

            ExeConfigurationFileMap configMap = new ExeConfigurationFileMap();
            configMap.ExeConfigFilename = UserConfigurationPath;
            configMap.LocalUserConfigFilename = UserConfigurationPath;
            configMap.RoamingUserConfigFilename = UserConfigurationPath;
            var cfg = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.PerUserRoamingAndLocal);

            return GetUserSettingsSection(cfg);
        }

        private bool IsUserSetting(SettingsProperty property)
        {
            foreach (DictionaryEntry d in property.Attributes)
            {
                if (((Attribute)d.Value) is UserScopedSettingAttribute)
                {
                    return true;
                }
            }
            return false;
        }

        public override void Initialize(string name, NameValueCollection values)
        {
            if (String.IsNullOrEmpty(name))
            {
                name = "CustomSettingsProvider";
            }

            base.Initialize(name, values);
        }

        /*public override SettingsPropertyValue GetPreviousVersion(SettingsContext context, SettingsProperty property)
        {
            return base.GetPreviousVersion(context, property);
        }

        public new void Reset(SettingsContext context)
        {
            base.Reset(context);
        }

        public new void Upgrade(SettingsContext context, SettingsPropertyCollection properties)
        {
            base.Upgrade(context, properties);
        }*/

        public override void SetPropertyValues(SettingsContext context, SettingsPropertyValueCollection collection)
        {
            //base.SetPropertyValues(context, collection);
            var cfg = GetSharedConfiguration();
            ClientSettingsSection appSettings = GetApplicationSettingsSection(cfg);
            var sets = collection.Cast<SettingsPropertyValue>().Where(x => !IsUserSetting(x.Property));
            SaveModifiedSettings(appSettings, sets);

            ClientSettingsSection userSettings = GetUserSettings(true);
            sets = collection.Cast<SettingsPropertyValue>().Where(x => IsUserSetting(x.Property));
            SaveModifiedSettings(userSettings, sets);
        }

        private void SaveModifiedSettings(ClientSettingsSection settings, IEnumerable<SettingsPropertyValue> sets)
        {
            bool ismod = false;
            foreach (var s in sets.Where(x => HasChanged(x)))
            {
                ismod = true;
                OverwriteSetting(settings, s.Name, s.SerializedValue.ToString());
            }

            if (ismod)
            {
                settings.CurrentConfiguration.Save(ConfigurationSaveMode.Modified);
            }
        }

        private bool HasChanged(SettingsPropertyValue x)
        {
            return _valuesCache[x.Name] != x.PropertyValue;
        }

        private void OverwriteSetting(ClientSettingsSection section, string settingName, string newValue)
        {
            var val = section.Settings.Get(settingName).Value;
            val.ValueXml.FirstChild.Value = newValue;

            // Forces the setting to be marked as updated as it's done in the ValueXml setter.
            // If not, the setting would be considered as unchanged as we directly changed the underlying value instead above.
            val.ValueXml = val.ValueXml;
        }

        public SettingsPropertyValue GetPreviousVersion(SettingsContext context, SettingsProperty property)
        {
            throw new NotImplementedException();
        }

        public void Reset(SettingsContext context)
        {
            throw new NotImplementedException();
        }

        public void Upgrade(SettingsContext context, SettingsPropertyCollection properties)
        {
            throw new NotImplementedException();
        }
    }
}
