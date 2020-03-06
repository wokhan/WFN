using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Security.Policy;
using System.Security.Principal;

namespace Wokhan.WindowsFirewallNotifier.Common.Helpers
{
    public class CustomSettingsProvider : SettingsProvider/*, IApplicationSettingsProvider*/
    {
        //Note: .NET framework has no easy way to get the types right, so our cache will be filled with strings.
        private Dictionary<string, object> _valuesCache = new Dictionary<string, object>();

        private static readonly string SectionName = "Wokhan.WindowsFirewallNotifier.Common.Settings";
        private static readonly string applicationSectionName = "applicationSettings";
        private static readonly string userSectionName = "userSettings";
        public static readonly string SharedConfigurationPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WFN.config");
        public static readonly string UserConfigurationPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Wokhan Solutions", "WFN", "user.config");
        public static readonly string UserLocalConfigurationPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Wokhan Solutions", "WFN", "user.config");

        public override string ApplicationName
        {
            get { return "WFNPotato"; } //FIXME: Erm...?
            set { }
        }

        public override SettingsPropertyValueCollection GetPropertyValues(SettingsContext context, SettingsPropertyCollection collection)
        {
            _valuesCache.Clear();

            var r = new SettingsPropertyValueCollection();

            Configuration cfg = null;

            var configMap = new ExeConfigurationFileMap();
            configMap.ExeConfigFilename = SharedConfigurationPath;
            if (!WindowsIdentity.GetCurrent().IsSystem)
            {
                //Only user settings for non-SYSTEM
                configMap.LocalUserConfigFilename = UserLocalConfigurationPath;
                configMap.RoamingUserConfigFilename = UserConfigurationPath;

                cfg = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.PerUserRoamingAndLocal);
                if (!cfg.HasFile)
                {
                    cfg = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.PerUserRoaming);
                }
            }

            if (cfg == null || !cfg.HasFile)
            {
                cfg = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);
                if (!cfg.HasFile)
                {
                    throw new ApplicationException("Configuration file missing!");
                }
            }

            try
            {
                ClientSettingsSection appSettings = GetApplicationSettingsSection(cfg);
                var sets = collection.Cast<SettingsProperty>().Where(x => !IsUserSetting(x));
                ExtractSettings(sets, r, appSettings);

                ClientSettingsSection userInitialSettings = GetUserSettingsSection(cfg);
                sets = collection.Cast<SettingsProperty>().Where(x => IsUserSetting(x));
                ExtractSettings(sets, r, userInitialSettings);
            }
            catch ( Exception e )
            {
                Console.WriteLine("Error loading config");
            }

            return r;
        }

        private void ExtractSettings(IEnumerable<SettingsProperty> sets, SettingsPropertyValueCollection r, ClientSettingsSection newValues)
        {
            if (newValues == null)
            {
                throw new ApplicationException("Configuration file is corrupt!");
            }
            foreach (var s in sets)
            {
                // need to provide default value for missing key sections in case something is not there
                var value = newValues.Settings.Get(s.Name)?.Value?.ValueXml?.FirstChild?.Value ?? s.DefaultValue;
                _valuesCache[s.Name] = value;
                r.Remove(s.Name);
                r.Add(new SettingsPropertyValue(new SettingsProperty(s)) { IsDirty = false, SerializedValue = value });
            }
        }

        private static ClientSettingsSection GetApplicationSettingsSection(Configuration cfg)
        {
            try
            {
                return (ClientSettingsSection)cfg.GetSectionGroup(applicationSectionName)?.Sections[SectionName];
            }
            catch (ConfigurationErrorsException e)
            {
                LogHelper.Warning("Errors while loading application settings:");
                foreach (var error in e.Errors)
                {
                    LogHelper.Warning(error.ToString());
                }
                throw;
            }
        }

        private static ClientSettingsSection GetUserSettingsSection(Configuration cfg)
        {
            try
            {
                return (ClientSettingsSection)cfg.GetSectionGroup(userSectionName)?.Sections[SectionName];
            }
            catch (ConfigurationErrorsException e)
            {
                LogHelper.Warning("Error while loading user settings:");
                foreach (var error in e.Errors)
                {
                    LogHelper.Warning(error.ToString());
                }
                throw;
            }
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

        private bool IsRoamingSetting(SettingsProperty property)
        {
            foreach (DictionaryEntry d in property.Attributes)
            {
                if ((((Attribute)d.Value) is SettingsManageabilityAttribute) && (((SettingsManageabilityAttribute)d.Value).Manageability == SettingsManageability.Roaming))
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
            if (WindowsIdentity.GetCurrent().IsSystem)
            {
                //Never save settings for SYSTEM
                return;
            }

            ExeConfigurationFileMap configMap = new ExeConfigurationFileMap();
            configMap.ExeConfigFilename = SharedConfigurationPath;
            configMap.LocalUserConfigFilename = UserLocalConfigurationPath;
            configMap.RoamingUserConfigFilename = UserConfigurationPath;

            //Create user config files if they don't exist yet
            if (!File.Exists(UserConfigurationPath))
            {
                var sharedcfg = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.PerUserRoamingAndLocal);
                sharedcfg.Save(ConfigurationSaveMode.Minimal);
            }

            var cfg = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.PerUserRoamingAndLocal);
            ClientSettingsSection appSettings = GetApplicationSettingsSection(cfg);
            var sets = collection.Cast<SettingsPropertyValue>().Where(x => !IsUserSetting(x.Property) && !IsRoamingSetting(x.Property));
            SaveModifiedSettings(appSettings, sets);

            ClientSettingsSection userSettings = GetUserSettingsSection(cfg);
            sets = collection.Cast<SettingsPropertyValue>().Where(x => IsUserSetting(x.Property) && !IsRoamingSetting(x.Property));
            SaveModifiedSettings(userSettings, sets);

            //Now save the roaming settings
            cfg = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.PerUserRoaming);
            appSettings = GetApplicationSettingsSection(cfg);
            sets = collection.Cast<SettingsPropertyValue>().Where(x => !IsUserSetting(x.Property) && IsRoamingSetting(x.Property));
            SaveModifiedSettings(appSettings, sets);

            userSettings = GetUserSettingsSection(cfg);
            sets = collection.Cast<SettingsPropertyValue>().Where(x => IsUserSetting(x.Property) && IsRoamingSetting(x.Property));
            SaveModifiedSettings(userSettings, sets);
        }

        private void SaveModifiedSettings(ClientSettingsSection settings, IEnumerable<SettingsPropertyValue> sets)
        {
            if (settings == null)
            {
                throw new ApplicationException("Configuration settings are corrupt!");
            }
            bool ismod = false;
            foreach (var s in sets.Where(x => HasChanged(x)))
            {
                ismod = true;
                OverwriteSetting(settings, s.Name, s.SerializedValue.ToString());
            }

            if (ismod)
            {
                try
                {
                    settings.CurrentConfiguration.Save(ConfigurationSaveMode.Modified);
                }
                catch (ConfigurationErrorsException e)
                {
                    LogHelper.Warning("Error while saving user settings:");
                    foreach (var error in e.Errors)
                    {
                        LogHelper.Warning(error.ToString());
                    }
                    throw;
                }
            }
        }

        private bool HasChanged(SettingsPropertyValue x)
        {
            return !x.PropertyValue.ToString().Equals(_valuesCache[x.Name]);
        }

        private void OverwriteSetting(ClientSettingsSection section, string settingName, string newValue)
        {
            var val = section.Settings.Get(settingName).Value;
            val.ValueXml.FirstChild.Value = newValue;

            // Forces the setting to be marked as updated as it's done in the ValueXml setter.
            // If not, the setting would be considered as unchanged as we directly changed the underlying value instead above.
            val.ValueXml = val.ValueXml;
        }

        /*public SettingsPropertyValue GetPreviousVersion(SettingsContext context, SettingsProperty property)
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
        }*/
    }
}
