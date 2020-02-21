using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Wokhan.WindowsFirewallNotifier.Notifier.UI.Windows
{

    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class ActivityWindow : Window

    {
        private readonly NotificationWindow notifierWindow;

        private bool HasWindowMoved = false;


        private double ExpectedTop
        {
            get { return HasWindowMoved ? this.Top : SystemParameters.WorkArea.Height - this.ActualHeight; }
        }

        private double ExpectedLeft
        {
            get { return HasWindowMoved ? this.Left : SystemParameters.WorkArea.Width - this.ActualWidth; }
        }

        private double ExpectedWidth
        {
            get { return SystemParameters.WorkArea.Width - this.ExpectedLeft; }
        }

        public static ActivityWindow Init(NotificationWindow window)
        {
            ActivityWindow factory = new ActivityWindow(window);
            return factory;
        }

        private ActivityWindow() { }
        private ActivityWindow(NotificationWindow window)
        {
            notifierWindow = window;

            InitializeComponent();

            ShowInTaskbar = false;
        }

        public void ShowIt()
        {
            this.Show();

            // initial position needs to be calculated after Show()
            Top = ExpectedTop;
            Left = ExpectedLeft;

            Topmost = true;
        }

        public void HideIt()
        {
            this.Hide();
        }

        private Point MouseDownPos;
        private void StackPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Converts a relative position to screen coordinates
            MouseDownPos = PointToScreen(e.GetPosition(this));
        }

        private void StackPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point p = PointToScreen(e.GetPosition(this));
                double deltaX = p.X - MouseDownPos.X;
                double deltaY = p.Y - MouseDownPos.Y;
                this.Top = Top + deltaY;
                this.Left = Left + deltaX;
                MouseDownPos = p;
                HasWindowMoved = true;
                System.Console.WriteLine($"topY {Top}, leftX {Left}, pPos={p}, delta={deltaX},{deltaY}");
            }
        }

        [DllImport("User32.dll")]
        static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("gdi32.dll")]
        static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        [DllImport("user32.dll")]
        static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);

        private Point ConvertPixelsToUnits(int x, int y)
        {
            // get the system DPI
            IntPtr dDC = GetDC(IntPtr.Zero); // Get desktop DC
            int dpi = GetDeviceCaps(dDC, 88);
            bool rv = ReleaseDC(IntPtr.Zero, dDC);

            // WPF's physical unit size is calculated by taking the 
            // "Device-Independant Unit Size" (always 1/96)
            // and scaling it by the system DPI
            double physicalUnitSize = (1d / 96d) * (double)dpi;
            Point wpfUnits = new Point(physicalUnitSize * (double)x,
                physicalUnitSize * (double)y);

            return wpfUnits;
        }
    }
}
