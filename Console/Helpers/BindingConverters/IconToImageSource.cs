using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace WFNConsole.Helpers.BindingConverters
{
    [ValueConversion(typeof(Icon), typeof(ImageSource))]
    public class IconToImageSource : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((Icon)value).AsImageSource();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}