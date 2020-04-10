using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Security.Principal;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;

namespace Wokhan.WindowsFirewallNotifier.Common.Config
{
    /// <summary>
    /// Implements now the LocalFileSettingsProvider which provides the standard implemention which also has Versioning and Reset.
    /// </summary>
    /// <remarks>
    /// - Name of the global settings file must conform "WFN.dll.config" to be found (provider expects this)
    /// - User settings are stored under AppData\Local\Wokhan Solutions\WFN_URL.....\1.0.0.x\user.config
    /// - Roaming property was removed from all settings (could be added again when required as the implmentation supports roaming as well)
    /// </remarks>
    public class CustomSettingsProvider : LocalFileSettingsProvider
    {
        public static string ExeConfigurationPath { get; } = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None).FilePath;
        public static string RoamingConfigurationPath { get; } = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoaming).FilePath;
        public static string UserLocalConfigurationPath { get; } = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath;

        public override string Name => "CustomSettingsProvider";
        public override string ApplicationName { get => "WFN"; set { } }

        public CustomSettingsProvider()
        {

        }

        public static void SaveAppSettingsFile()
        {
            Configuration appConf = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            appConf.Save();
        }
    }
}
