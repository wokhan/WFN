using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Messages = Wokhan.WindowsFirewallNotifier.Common.Properties.Resources;
using System.Windows.Media.Imaging;
using Wokhan.WindowsFirewallNotifier.Common.Config;
using System.Windows.Shapes;

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
            InitWindowsMouseEvents();

            ShowInTaskbar = false;  // hide the icon in the taskbar

            ClickableIcon.Source = ICON_NORMAL;
            ClickableIcon.ContextMenu = InitMenu();
            ClickableIcon.ToolTip = Messages.ActivityWindow_ClickableIcon_Tooltip;

            notifierWindow.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(notifierWindow.NbConnectionsAfter))
                {
                    RefreshClickableIcon();
                }
            };
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
            ContextMenu contextMenu = new ContextMenu();
            addMenuItem(contextMenu, Messages.ActivityWindow_ShowNotifier, (s, e) => notifierWindow.RestoreWindowState());
            addMenuItem(contextMenu, Messages.ActivityWindow_OpenConsole, (s, e) => notifierWindow.ShowConsole());
            addMenuItem(contextMenu, Messages.ActivityWindow_DiscardAndClose, (s, e) => { notifierWindow.Close(); Close(); });
            addMenuItem(contextMenu, Messages.ActivityWindow_HideThisWindow, (s, e) => Hide());
            addMenuItem(contextMenu, SetOrientationGetMessage(), (s, e) => ((MenuItem)s).Header = SetOrientationGetMessage(true));

            return contextMenu;
        }

        private string SetOrientationGetMessage(bool toggle = false)
        {
            if (toggle)
            {
                Settings.Default.ActivityWindow_Orientation = (Settings.Default.ActivityWindow_Orientation == Orientation.Horizontal ? Orientation.Vertical : Orientation.Horizontal);
            }

            return (Settings.Default.ActivityWindow_Orientation == Orientation.Horizontal ? Messages.ActivityWindow_OrientationVertical : Messages.ActivityWindow_OrientationHorizontal);
        }

        private void addMenuItem(ContextMenu cm, string caption, RoutedEventHandler eh)
        {
            MenuItem mi = new MenuItem
            {
                Header = caption
            };
            mi.Click += eh;
            cm.Items.Add(mi);
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

        private void ToggleGreen()
        {
            var gradStop = ((RadialGradientBrush)GreenLight.Fill).GradientStops[0];
            gradStop.Color = Colors.Green.Equals(gradStop.Color) ? Colors.White : Colors.Green;
        }

        private void ToggleRed()
        {
            var gradStop = ((RadialGradientBrush)RedLight.Fill).GradientStops[0];
            gradStop.Color = Colors.Red.Equals(gradStop.Color) ? Colors.White : Colors.Red;
        }

        private async void ToggleLightsTask(Ellipse control, int waitMillis)
        {
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
                ToggleLightsTask(GreenLight, 200);
            }
            else
            {
                ToggleLightsTask(RedLight, 200);
            }
        }
    }
}
