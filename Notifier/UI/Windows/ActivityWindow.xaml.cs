using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using WinForms = System.Windows.Forms;
using Wokhan.WindowsFirewallNotifier.Common.Properties;
using Messages = Wokhan.WindowsFirewallNotifier.Common.Properties.Resources;
using System.Diagnostics;
using System.Windows.Media.Imaging;
using Wokhan.WindowsFirewallNotifier.Common;

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

        readonly Orientation WindowAlignment = Settings.Default.ActivityWindow_Orientation;

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

            if (WindowAlignment.Equals(Orientation.Horizontal))
            {
                // switch orientation from vertical
                double origWidth = this.Width;
                this.Width = this.Height;
                this.Height = origWidth;
                ControlsContainer.Orientation = Orientation.Horizontal;
                ControlsContainer.Height = this.Height;
                ControlsContainer.Width = this.Width;
            }

            InitWindowsMouseEvents();

            ShowInTaskbar = false;  // hide the icon in the taskbar

            ClickableIcon.Source = ICON_NORMAL;
            ClickableIcon.ContextMenu = InitMenu();
            ClickableIcon.ToolTip = Messages.ActivityWindow_ClickableIcon_Tooltip;

            notifierWindow.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(notifierWindow.NbConnectionsAfter))
                {
                    RefreshClickableIcon();
                }
            };
        }

        private void InitWindowsMouseEvents()
        {
            Point previousWindowPos = new Point(this.Left, this.Top);
            Point actualWindowsPos = previousWindowPos;
            MouseLeftButtonUp += (object sender, MouseButtonEventArgs e) =>
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
            MouseLeftButtonDown += (object sender, MouseButtonEventArgs e) =>
            {
                DragMove();
            };
        }


        private ContextMenu InitMenu()
        {
            void MenuShow_Click(object Sender, RoutedEventArgs e)
            {
                notifierWindow.RestoreWindowState();
            }
            void MenuClose_Click(object Sender, EventArgs e)
            {
                notifierWindow.Close();
                Close();
            }
            void MenuConsole_Click(object Sender, EventArgs e)
            {
                notifierWindow.ShowConsole();
            }
            void MenuHide_Click(object Sender, EventArgs e)
            {
                Hide();
            }
            void addMenuItem(ContextMenu cm, string caption, RoutedEventHandler eh)
            {
                MenuItem mi = new MenuItem
                {
                    Header = caption
                };
                mi.Click += eh;
                cm.Items.Add(mi);
            }
            ContextMenu contextMenu = new ContextMenu();
            addMenuItem(contextMenu, Messages.ActivityWindow_ShowNotifier, MenuShow_Click);
            addMenuItem(contextMenu, Messages.ActivityWindow_OpenConsole, MenuConsole_Click);
            addMenuItem(contextMenu, Messages.ActivityWindow_DiscardAndClose, MenuClose_Click);
            addMenuItem(contextMenu, Messages.ActivityWindow_HideThisWindow, MenuHide_Click);

            return contextMenu;
        }

        public new void Show()
        {
            base.Show();
            // initial position needs to be calculated after Show()
            void initWindowsPosition()
            {
                Point defaultPos = Settings.Default.ActivityWindow_Position;
                if (defaultPos == new Point(0d, 0d) || defaultPos == null)
                {
                    defaultPos.X = SystemParameters.WorkArea.Width - this.ActualWidth;
                    defaultPos.Y = SystemParameters.WorkArea.Height / 2;
                    Settings.Default.ActivityWindow_Position = defaultPos;
                    Settings.Default.Save();
                }
                Top = defaultPos.Y;
                Left = defaultPos.X;
            }

            initWindowsPosition();
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
            GreenLight.Background = (Brushes.DarkGreen.Equals(GreenLight.Background)) ? Brushes.LightGreen : Brushes.DarkGreen;
        }
        private void ToggleRed()
        {
            RedLight.Background = (Brushes.DarkRed.Equals(RedLight.Background)) ? Brushes.Red : Brushes.DarkRed;
        }

        private async void ToggleLightsTask(Border control, int waitMillis)
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
            else
            {
                if (ICON_NORMAL != ClickableIcon.Source)
                {
                    ClickableIcon.Source = ICON_NORMAL;
                }
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
