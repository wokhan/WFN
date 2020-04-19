﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Security.Principal;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;

namespace Wokhan.WindowsFirewallNotifier.Common.Config
{
    public class CustomSettingsProvider : SettingsProvider/*, IApplicationSettingsProvider*/
    {
        //Note: .NET framework has no easy way to get the types right, so our cache will be filled with strings.
        private Dictionary<string, object> _valuesCache = new Dictionary<string, object>();

        private const string SETTINGS_KEY = "Wokhan.WindowsFirewallNotifier.Common.Config.Settings";
        private const string APP_SETTINGS = "applicationSettings";
        private const string USER_SETTINGS = "userSettings";

        public static string SharedConfigurationPath { get; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory ?? string.Empty, "WFN.config");
        public static string UserConfigurationPath { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Wokhan Solutions", "WFN", "user.config");
        public static string UserLocalConfigurationPath { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Wokhan Solutions", "WFN", "user.config");

        public override string Name => "CustomSettingsProvider";
        public override string ApplicationName { get => "WFN"; set { } }

        public override SettingsPropertyValueCollection GetPropertyValues(SettingsContext context, SettingsPropertyCollection collection)
        {
            _valuesCache.Clear();

            var r = new SettingsPropertyValueCollection();

            Configuration? cfg = null;

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

            if (cfg is null || !cfg.HasFile)
            {
                cfg = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);
                if (!cfg.HasFile)
                {
                    throw new ApplicationException("Configuration file missing!");
                }
            }

            try
            {
                ClientSettingsSection? appSettings = GetApplicationSettingsSection(cfg);
                var sets = collection.Cast<SettingsProperty>().Where(x => !IsUserSetting(x));
                ExtractSettings(sets, r, appSettings);

                ClientSettingsSection? userInitialSettings = GetUserSettingsSection(cfg);
                sets = collection.Cast<SettingsProperty>().Where(x => IsUserSetting(x));
                ExtractSettings(sets, r, userInitialSettings);
            }
            catch
            {
                Console.WriteLine("Error loading config");
            }

            return r;
        }

        private void ExtractSettings(IEnumerable<SettingsProperty> sets, SettingsPropertyValueCollection r, ClientSettingsSection? newValues)
        {
            if (newValues is null)
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

        private static ClientSettingsSection? GetApplicationSettingsSection(Configuration cfg) => GetSettingsSection(cfg, APP_SETTINGS);

        private static ClientSettingsSection? GetUserSettingsSection(Configuration cfg) => GetSettingsSection(cfg, USER_SETTINGS);

        private static ClientSettingsSection? GetSettingsSection(Configuration cfg, string sectionName)
        {
            try
            {
                return (ClientSettingsSection?)cfg.GetSectionGroup(USER_SETTINGS)?.Sections[SETTINGS_KEY];
            }
            catch (ConfigurationErrorsException e)
            {
                DumpErrors(e, "loading" + sectionName);
                throw;
            }

        }

        private bool IsUserSetting(SettingsProperty property)
        {
            return property.Attributes
                           .Cast<DictionaryEntry>()
                           .Any(d => d.Value is UserScopedSettingAttribute);
        }

        private bool IsRoamingSetting(SettingsProperty property)
        {
            return property.Attributes
                           .Cast<DictionaryEntry>()
                           .Any(d => d.Value is SettingsManageabilityAttribute attribute && attribute.Manageability == SettingsManageability.Roaming);
        }

        public override void SetPropertyValues(SettingsContext context, SettingsPropertyValueCollection collection)
        {
            if (WindowsIdentity.GetCurrent().IsSystem)
            {
                //Never save settings for SYSTEM
                return;
            }

            var configMap = new ExeConfigurationFileMap();
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
            ClientSettingsSection? appSettings = GetApplicationSettingsSection(cfg);
            var sets = collection.Cast<SettingsPropertyValue>().Where(x => !IsUserSetting(x.Property) && !IsRoamingSetting(x.Property));
            SaveModifiedSettings(appSettings, sets);

            ClientSettingsSection? userSettings = GetUserSettingsSection(cfg);
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

        private void SaveModifiedSettings(ClientSettingsSection? settings, IEnumerable<SettingsPropertyValue> sets)
        {
            if (settings is null)
            {
                throw new ApplicationException("Configuration settings are corrupt!");
            }
            var ismod = false;
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
                    DumpErrors(e, "saving");
                    throw;
                }
            }
        }

        private static void DumpErrors(ConfigurationErrorsException e, string action)
        {
            LogHelper.Warning($"Error while {action} settings:");
            foreach (var error in e.Errors)
            {
                LogHelper.Warning(error.ToString());
            }
        }

        private bool HasChanged(SettingsPropertyValue x)
        {
            return !_valuesCache.TryGetValue(x.Name, out var prev) && (prev?.Equals(x.PropertyValue?.ToString()) ?? true);
        }

        private static void OverwriteSetting(ClientSettingsSection section, string settingName, string newValue)
        {
            var setting = section.Settings.Get(settingName);
            if (setting is null)
            {
                LogHelper.Debug("Setting is not part of the current section. Skipping.");
                return;
            }
            var val = setting.Value;
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
