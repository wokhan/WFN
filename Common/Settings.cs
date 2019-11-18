using System.Configuration;
//using System.IO;
//using System.Xml;
//using System.Xml.Serialization;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;

namespace Wokhan.WindowsFirewallNotifier.Common
{
    [SettingsProvider(typeof(CustomSettingsProvider))]
    public sealed partial class Settings
    {
        public Settings() : base()
        {

        }
        public bool EnableServiceDetection
        {
            get { return (bool)this["EnableServiceDetectionGlobal"]; }
            set { this["EnableServiceDetectionGlobal"] = value; }
        }

        public bool UseBlockRules
        {
            get { return (bool)this["UseBlockRulesGlobal"]; }
            set { this["UseBlockRulesGlobal"] = value; }
        }
    }
}
