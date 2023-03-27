using Microsoft.Maps.MapControl.WPF;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

using Wokhan.Collections.Generic.Extensions;
using Wokhan.WindowsFirewallNotifier.Common.Net.GeoLocation;

namespace Wokhan.WindowsFirewallNotifier.Console.UI.BindingConverters
{
    [ValueConversion(typeof(IEnumerable<GeoLocation>), typeof(LocationCollection))]
    internal class GeoLocationsToBingLocationCollection : IValueConverter
    {
        private static LocationCollection _emptyCollection = new();
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IEnumerable<GeoLocation> locations)
            {
                var mapsLocations = new LocationCollection();
                mapsLocations.AddAll(locations.Select(location => new Location(location.Latitude!.Value, location.Longitude!.Value)));
                return mapsLocations;
            }

            return _emptyCollection;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
