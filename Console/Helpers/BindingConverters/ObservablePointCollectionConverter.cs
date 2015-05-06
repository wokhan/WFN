using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Wokhan.WindowsFirewallNotifier.Console.Helpers.BindingConverters
{
    [ValueConversion(typeof(ObservableCollection<Point>), typeof(PointCollection))]
    public class ObservablePointCollectionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return new PointCollection(((ObservableCollection<Point>)value).Select(p => p));
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}
