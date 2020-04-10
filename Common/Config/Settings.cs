using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Wokhan.WindowsFirewallNotifier.Common.Config
{
    /// <summary>
    /// Uses now the default LocalFileSettingsProvider to handle settings.
    /// </summary>
    /// <remarks>
    ///   Name of the global settings file must conform to "WFN.dll.config" to be found (provider expects this)
    ///   Roaming settings: Open Settings Designer > select a setting then open the Properties window > Roaming true/false
    ///   All settings have now Roaming=false
    /// </remarks>

    [SettingsProvider(typeof(CustomSettingsProvider))]
    public sealed partial class Settings : ApplicationSettingsBase
    {

        public Settings() : base()
        {

        }
        public override SettingsProviderCollection Providers => base.Providers;

        public void SaveAppSettings()
        {
            CustomSettingsProvider.SaveAppSettingsFile();
        }

        public bool EnableServiceDetection
        {
            get { return (bool)this[nameof(EnableServiceDetectionGlobal)]; }
            set { this[nameof(EnableServiceDetectionGlobal)] = value; }
        }

        public bool UseBlockRules
        {
            get { return (bool)this[nameof(UseBlockRulesGlobal)]; }
            set { this[nameof(UseBlockRulesGlobal)] = value; }
        }

    }
}
