using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WFNConsole.Helpers
{
    static class Extensions
    {
        public static ImageSource AsImageSource(this Icon ic)
        {
            return Imaging.CreateBitmapSourceFromHIcon(ic.Handle, new Int32Rect(0, 0, ic.Width, ic.Height), BitmapSizeOptions.FromEmptyOptions());
        }

    }
}
