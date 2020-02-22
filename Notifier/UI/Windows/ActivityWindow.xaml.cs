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
using Wokhan.WindowsFirewallNotifier.Notifier.Helpers;

namespace Wokhan.WindowsFirewallNotifier.Notifier.UI.Windows
{

    /*
     Activity window with lights for allow/block activities.
    */
    public partial class ActivityWindow : Window

    {
        private readonly NotificationWindow notifierWindow;

        private bool HasPositionChanged = false;  // used to remember the position when moved by a user
        private bool DisableClick = false;

        public enum WindowAlignmentEnum
        {
            Horizontal, Vertical
        }
        readonly WindowAlignmentEnum WindowAlignment = WindowAlignmentEnum.Horizontal;

        private double ExpectedTop
        {
            get { return HasPositionChanged ? this.Top : SystemParameters.WorkArea.Height - this.ActualHeight; }
        }

        private double ExpectedLeft
        {
            get { return HasPositionChanged ? this.Left : SystemParameters.WorkArea.Width - this.ActualWidth; }
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
            if (WindowAlignment.Equals(WindowAlignmentEnum.Horizontal))
            {
                // switch orientation
                double origWidth = this.Width;
                this.Width = this.Height;
                this.Height = origWidth;
                ControlsContainer.Orientation = Orientation.Horizontal;
                ControlsContainer.Height = this.Height;
                ControlsContainer.Width = this.Width;
            }

            ShowInTaskbar = false;  // hide the task icon

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

        private void ToggleGreen()
        {
            GreenLight.Background = (Brushes.DarkGreen.Equals(GreenLight.Background)) ? Brushes.LightGreen : Brushes.DarkGreen;
        }
        private void ToggleRed()
        {
            RedLight.Background = (Brushes.DarkRed.Equals(RedLight.Background)) ? Brushes.Red : Brushes.DarkRed;
        }

        private async void ToggleLightsTask(Border control, int waitMillis)
        {
            Brush origBrush = control.Background;
            void action()
            {
                if (GreenLight.Equals(control))
                {
                    ToggleGreen();
                } 
                else if (RedLight.Equals(control))
                {
                    ToggleRed();
                }
            };

            for (int i = 0; i < 2; i++)
            {
                Dispatcher.Invoke(action);
                await Task.Delay(waitMillis).ConfigureAwait(false);
            }
        }

        public enum ActivityEnum
        {
            Allowed, Blocked
        }
        public void ShowActivity(ActivityEnum activity)
        {
            if (ActivityEnum.Allowed.Equals(activity))
            {
                ToggleLightsTask(GreenLight, 200);
            }
            else
            {
                ToggleLightsTask(RedLight, 200);
            }
        }

        private Point MouseDownPos;
        private void StackPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Converts a relative position to screen coordinates
            MouseDownPos = PointToScreen(e.GetPosition(this));
            DisableClick = false;
        }

        private void Button_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!DisableClick)
            {
                ShowActivity(ActivityEnum.Allowed);
                notifierWindow.RestoreWindowState();
            }
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
                HasPositionChanged = true;
                DisableClick = true;
            }
        }
    }
}
