using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Messages = Wokhan.WindowsFirewallNotifier.Common.Properties.Resources;
using System.Windows.Media.Imaging;
using Wokhan.WindowsFirewallNotifier.Common.Config;
using System.Windows.Threading;

namespace Wokhan.WindowsFirewallNotifier.Notifier.UI.Windows
{

    /*
        Activity window with lights for allow/block activities.
     
        Author: 
            harrwiss
    */
    public partial class ActivityWindow : Window

    {
        private readonly NotificationWindow notifierWindow;

        private readonly BitmapImage ICON_NORMAL = new BitmapImage(new Uri(@"/Notifier;component/Resources/TrayIcon22.ico", UriKind.Relative));
        private readonly BitmapImage ICON_BLOCKED = new BitmapImage(new Uri(@"/Notifier;component/Resources/TrayIcon21.ico", UriKind.Relative));
        
        private readonly DispatcherTimer ResetGreenLightTimer = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(200), IsEnabled = false };
        private readonly DispatcherTimer ResetRedLightTimer = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(200), IsEnabled = false };

        public ActivityWindow(NotificationWindow window, bool showWhenCreated)
        {
            notifierWindow = window;

            InitializeComponent();

            InitWindowsMouseEvents();

            ClickableIcon.Source = ICON_NORMAL;
            ClickableIcon.ContextMenu = InitMenu();
            ClickableIcon.ToolTip = Messages.ActivityWindow_ClickableIcon_Tooltip;

            ResetGreenLightTimer.Tick += GreenTurnOff;
            ResetRedLightTimer.Tick += RedTurnOff;

            notifierWindow.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(notifierWindow.NbConnectionsAfter))
                {
                    RefreshClickableIcon();
                }
            };

            if (showWhenCreated)
            {
                Show();
            }
        }

        private void InitWindowsMouseEvents()
        {
            Point previousWindowPos = new Point(this.Left, this.Top);
            Point actualWindowsPos = previousWindowPos;
            MouseLeftButtonUp += (s, e) =>
            {
                actualWindowsPos = new Point(this.Left, this.Top);
                if (actualWindowsPos.Equals(previousWindowPos))
                {
                    ShowActivity(ActivityEnum.Allowed);
                    ClickableIcon.Source = ICON_NORMAL;
                    if (WindowState.Minimized == notifierWindow.WindowState)
                    {
                        notifierWindow.RestoreWindowState();
                    }
                    else
                    {
                        notifierWindow.HideWindowState();
                    }
                }
                else
                {
                    previousWindowPos = actualWindowsPos;
                    Settings.Default.ActivityWindow_Position = actualWindowsPos;
                    Settings.Default.Save();
                }
            };
            MouseLeftButtonDown += (s, e) => DragMove();
        }


        private ContextMenu InitMenu()
        {
            ContextMenu contextMenu = new ContextMenu()
            {
                Items = {
                    CreateMenuItem(Messages.ActivityWindow_ShowNotifier, (s, e) => notifierWindow.RestoreWindowState()),
                    CreateMenuItem(Messages.ActivityWindow_OpenConsole, (s, e) => notifierWindow.ShowConsole()),
                    CreateMenuItem(Messages.ActivityWindow_HideThisWindow, (s, e) => Hide()),
                    CreateMenuItem(SetOrientationGetMessage(), (s, e) => ((MenuItem)s).Header = SetOrientationGetMessage(true)),
                    new Separator(),
                    CreateMenuItem(Messages.ActivityWindow_DiscardAndClose, (s, e) => { notifierWindow.Close(); Close(); })
                }
            };

            return contextMenu;
        }

        private static string SetOrientationGetMessage(bool toggle = false)
        {
            if (toggle)
            {
                Settings.Default.ActivityWindow_Orientation = (Settings.Default.ActivityWindow_Orientation == Orientation.Horizontal ? Orientation.Vertical : Orientation.Horizontal);
            }

            return (Settings.Default.ActivityWindow_Orientation == Orientation.Horizontal ? Messages.ActivityWindow_OrientationVertical : Messages.ActivityWindow_OrientationHorizontal);
        }

        private static MenuItem CreateMenuItem(string caption, RoutedEventHandler handler)
        {
            MenuItem mi = new MenuItem { Header = caption };
            mi.Click += handler;

            return mi;
        }


        public new void Show()
        {
            base.Show();
            // initial position needs to be calculated after Show()

            Point defaultPos = Settings.Default.ActivityWindow_Position;
            if (defaultPos == new Point(0d, 0d) || defaultPos == null)
            {
                defaultPos.X = SystemParameters.WorkArea.Width - this.ActualWidth;
                defaultPos.Y = SystemParameters.WorkArea.Height / 2;
                Settings.Default.ActivityWindow_Position = defaultPos;
            }
            Top = defaultPos.Y;
            Left = defaultPos.X;

            Topmost = true;
            ResetTopmostVisibility();

            Settings.Default.ActivityWindow_Shown = true;
            Settings.Default.Save();
        }

        public new void Hide()
        {
            base.Hide();
            Settings.Default.ActivityWindow_Shown = false;
            Settings.Default.Save();
        }

        private void GreenTurnOn()
        {
            ResetGreenLightTimer.Stop();
            Dispatcher.Invoke(() => ((RadialGradientBrush)GreenLight.Fill).GradientStops[0].Color = Colors.LightGreen);
            ResetGreenLightTimer.Start();
        }

        private void GreenTurnOff(object sender, EventArgs e)
        {
            ResetGreenLightTimer.Stop();
            ((RadialGradientBrush)GreenLight.Fill).GradientStops[0].Color = Colors.Green;
        }

        private void RedTurnOn()
        {
            ResetRedLightTimer.Stop();
            Dispatcher.Invoke(() => ((RadialGradientBrush)RedLight.Fill).GradientStops[0].Color = Colors.OrangeRed);
            ResetRedLightTimer.Start();
        }

        private void RedTurnOff(object sender, EventArgs e)
        {
            ResetRedLightTimer.Stop();
            ((RadialGradientBrush)RedLight.Fill).GradientStops[0].Color = Colors.Red;
        }

        public enum ActivityEnum
        {
            Allowed, Blocked
        }

        private void RefreshClickableIcon()
        {
            if (notifierWindow.NbConnectionsAfter > 0)
            {
                if (IsVisible && ClickableIcon.Source != ICON_BLOCKED)
                {
                    ClickableIcon.Source = ICON_BLOCKED;
                    ResetTopmostVisibility();
                }
            }
            else if (ClickableIcon.Source != ICON_NORMAL)
            {
                ClickableIcon.Source = ICON_NORMAL;
            }
        }

        private void ResetTopmostVisibility()
        {
            // make it topmost visible
            Visibility = Visibility.Hidden;
            Visibility = Visibility.Visible;

            //Activate(); // dont use it - moves the focus as well to the window
        }

        public void ShowActivity(ActivityEnum activity)
        {
            if (ActivityEnum.Allowed.Equals(activity))
            {
                GreenTurnOn();
            }
            else
            {
                RedTurnOn();
            }
        }
    }
}
