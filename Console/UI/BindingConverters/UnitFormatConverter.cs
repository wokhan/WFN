using System.Globalization;
using System.Windows.Data;

using Wokhan.Core;

namespace Wokhan.WindowsFirewallNotifier.Console.UI.BindingConverters;

// Should be moved to Wokhan.UI (it already exists there but with a wrong Wokhan.Core dependency)
public sealed class UnitFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        try
        {
            return UnitFormatter.FormatValue(System.Convert.ToDouble(value), parameter as string);
        }
        catch
        {
            return "#ERR";
        }
    }


    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}