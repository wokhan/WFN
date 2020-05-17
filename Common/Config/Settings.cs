using Microsoft.Extensions.Configuration;
using System;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;

namespace Wokhan.WindowsFirewallNotifier.Common.Config
{
    public sealed partial class Settings : ApplicationSettingsBase
    {
        private static IConfigurationRoot configuration;
        private static string applicationConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory ?? string.Empty, "settings.json");
        private static string userConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Wokhan Solutions", "WFN", "settings.json");

        public static event EventHandler<PropertyChangedEventArgs>? StaticPropertyChanged;

        public string ConfigurationPath => IsPortable ? applicationConfigPath : userConfigPath;

        public Settings() //: base()
        {
            Providers.Clear();
            PropertyChanged += Settings_PropertyChanged;
        }

        private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(IsPortable):
                    NotifyPropertyChanged(nameof(ConfigurationPath));
                    break;

                case nameof(AccentColor):
                    Application.Current.Resources["AccentColorBrush"] = AccentColor;
                    break;

                default:
                    break;
            }
        }

        static Settings()
        {
            configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                                                      // Read only application-level configuration
                                                      .AddJsonFile(applicationConfigPath, true, true)
                                                      // User overrides (if any, for current user)
                                                      .AddJsonFile(userConfigPath, true, true)
                                                      .Build();

            // Overrides "defaultInstance" member of the generated partial Settings class (to keep the designer)
            defaultInstance = configuration.Get<Settings>() ?? defaultInstance;
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

        public override void Save()
        {
            var userSettings = typeof(Settings).GetProperties()
                                               .Where(property => property.GetCustomAttribute<UserScopedSettingAttribute>() != null)
                                               .ToDictionary(property => property.Name, property => property.GetValue(this));

            if (this.IsPortable)
            {
                File.Delete(userConfigPath);
            }
            else
            {
                File.Delete(applicationConfigPath);
            }

            File.WriteAllText(ConfigurationPath, JsonSerializer.Serialize(userSettings, new JsonSerializerOptions() { IgnoreReadOnlyProperties = true, WriteIndented = true }));
        }

        public new void Reload()
        {
            defaultInstance = configuration.Get<Settings>();
            StaticPropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Default)));
        }

        private void NotifyPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            OnPropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
