using System;
using System.Configuration;
using System.Linq;

namespace Wokhan.WindowsFirewallNotifier.Common.Config
{
    // Doesn't work anymore :-/ (see comment below on OnSettingsLoaded)
    //[SettingsProvider(typeof(CustomSettingsProvider))]
    public sealed partial class Settings : ApplicationSettingsBase
    {
        private SettingsProvider provider;
        public Settings() : base()
        {
            provider = new CustomSettingsProvider();
            
            Providers.Clear();
            Providers.Add(provider);

        }

        // This is awesomely awful. But it fixes an issue with .Net Core 3.1 not taking the right config file 
        // (at least with latest modifications), while I could swear it was working last week without this... Anyway...
        protected override void OnSettingsLoaded(object sender, SettingsLoadedEventArgs e)
        {
            base.OnSettingsLoaded(sender, e);

            PropertyValues.Cast<SettingsPropertyValue>().ToList().ForEach(p => p.Property.Provider = provider);
        }

        public override SettingsProviderCollection Providers => base.Providers;

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
