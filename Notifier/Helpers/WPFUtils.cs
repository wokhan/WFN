using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Windows;
using System.Runtime.InteropServices;

namespace Wokhan.WindowsFirewallNotifier.Notifier.Helpers
{
    /**
     WPF utility methods.
      
      @author harrwiss
    */
    public static class WPFUtils
    {
        [DllImport("User32.dll")]
        static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("gdi32.dll")]
        static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        [DllImport("user32.dll")]
        static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);

        /**
         * Increase brightness of a brush.
         * 
         */
        public static SolidColorBrush LightenBrush(Brush brush, byte amount)
        {
            if (brush == null)
            {
                throw new ArgumentException(@"Brush parameter may not be null");
            }
            SolidColorBrush newBrush = (SolidColorBrush)brush.CloneCurrentValue();
            Color color = newBrush.Color;
            color.R += amount;
            color.G += amount;
            color.B += amount;
            newBrush.Color = color;
            return newBrush;
        }

        /**
        * Dispatch an action to be executed on the UI thread.
        * 
        * Example 1:
        *  action() {
        *        .. do something on the UI ..
        *  }
        *  DispatchUI(action);
        * 
        * Example 2:
        *  DispatchUI(() => {
        *    .. do something on the UI ..
        *  });
        * 
        * Parameters: 
        *  action - Method without parameters and return value or lambda expression</param>
        *  
        */
        public static void DispatchUI(Action action)
        {
            Application.Current.Dispatcher.Invoke(action);
        }


        public static Point ConvertPixelsToUnits(int x, int y)
        {
            // get the system DPI
            IntPtr dDC = GetDC(IntPtr.Zero); // Get desktop DC
            int dpi = GetDeviceCaps(dDC, 88);
            ReleaseDC(IntPtr.Zero, dDC);
            // WPF's physical unit size is calculated by taking the "Device-Independant Unit Size" (always 1/96)
            // and scaling it by the system DPI
            double physicalUnitSize = (1d / 96d) * (double)dpi;
            Point wpfUnits = new Point(physicalUnitSize * (double)x,
                physicalUnitSize * (double)y);

            return wpfUnits;
        }

    }
}
