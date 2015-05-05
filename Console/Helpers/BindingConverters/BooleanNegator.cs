﻿using System;
using System.Globalization;
using System.Windows.Data;

namespace Wokhan.WindowsFirewallNotifier.Console.Helpers.BindingConverters
{
    [ValueConversion(typeof(bool), typeof(bool))]
    public class BooleanNegator : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !((bool)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
