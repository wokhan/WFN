using System.Configuration;
//using System.IO;
//using System.Xml;
//using System.Xml.Serialization;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;

namespace Wokhan.WindowsFirewallNotifier.Common.Config
{
    [SettingsProvider(typeof(CustomSettingsProvider))]
    public sealed partial class Settings
    {
        public Settings() : base()
        {

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

        public static void OverrideSettingsFile(string fileName)
        {
            //AppDomain.CurrentDomain.SetupInformation.ConfigurationFile = fileName;
        }
    }
}
