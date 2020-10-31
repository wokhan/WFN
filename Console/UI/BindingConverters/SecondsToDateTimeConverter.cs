using System;
using System.Globalization;
using System.Windows.Data;

namespace Wokhan.WindowsFirewallNotifier.Console.UI.BindingConverters
{
    [ValueConversion(typeof(bool), typeof(bool))]
    public class SecondsToDateTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DateTime.Now.AddSeconds((long)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
