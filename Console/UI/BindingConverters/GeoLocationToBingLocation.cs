using Microsoft.Maps.MapControl.WPF;

using System;
using System.Globalization;
using System.Windows.Data;

using Wokhan.WindowsFirewallNotifier.Common.Net.GeoLocation;

namespace Wokhan.WindowsFirewallNotifier.Console.UI.BindingConverters
{
    [ValueConversion(typeof(GeoLocation), typeof(Location))]
    internal class GeoLocationToBingLocation : IValueConverter
    {
        private static Location _unknown = new Location();
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is GeoLocation location)
            {
                return new Location(location.Latitude ?? 0, location.Longitude ?? 0);
            }

            return _unknown;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
