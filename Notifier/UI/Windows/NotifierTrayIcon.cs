using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using Wokhan.WindowsFirewallNotifier.Common;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;
using Wokhan.WindowsFirewallNotifier.Common.Properties;
using Wokhan.WindowsFirewallNotifier.Notifier.Helpers;
using System.Diagnostics;

namespace Wokhan.WindowsFirewallNotifier.Notifier.UI.Windows
{
    /// <summary>
    /// Notifier tray icon shown when minimizing the Notifier Window.
    /// @Author: harrwiss
    /// </summary>
    public class NotifierTrayIcon
    {
        private readonly System.Windows.Forms.NotifyIcon trayIcon;
        private readonly System.Windows.Forms.ContextMenu contextMenu;
        private readonly System.Windows.Forms.MenuItem menuDiscard;
        private readonly System.Windows.Forms.MenuItem menuShow;
        private readonly System.Windows.Forms.MenuItem menuConsole;
        private readonly System.ComponentModel.IContainer components;

        private bool activityTipShown = false;

        private readonly NotificationWindow window;

        public static NotifierTrayIcon Init(NotificationWindow window)
        {
            NotifierTrayIcon factory = new NotifierTrayIcon(window);
            return factory;
        }

        private NotifierTrayIcon() { }
        private NotifierTrayIcon(NotificationWindow window)
        {
            //  https://docs.microsoft.com/en-us/dotnet/api/system.windows.forms.notifyicon?redirectedfrom=MSDN&view=netframework-4.8
            this.window = window;
            components = new System.ComponentModel.Container();

            menuShow = new System.Windows.Forms.MenuItem
            {
                Index = 0,
                Text = "&Show Notifier"
            };
            menuShow.Click += new System.EventHandler(MenuShow_Click);

            // TODO: maybe??
            //menuBlockSilently = new System.Windows.Forms.MenuItem
            //{
            //    Index = 0,
            //    Text = "&Exit and block silently"
            //};
            //menuBlockSilently.Click += new System.EventHandler(MenuBlockSilently_Click);

            menuConsole = new System.Windows.Forms.MenuItem
            {
                Index = 0,
                Text = "&Open Console"
            };
            menuConsole.Click += new System.EventHandler(MenuConsole_Click);

            menuDiscard = new System.Windows.Forms.MenuItem
            {
                Index = 0,
                Text = "&Discard and close"
            };
            menuDiscard.Click += new System.EventHandler(MenuClose_Click);

            contextMenu = new System.Windows.Forms.ContextMenu();
            contextMenu.MenuItems.AddRange(
                  new System.Windows.Forms.MenuItem[] { menuShow, menuConsole, menuDiscard });

            // Create the NotifyIcon. 
            trayIcon = new System.Windows.Forms.NotifyIcon(components)
            {
                Icon = Notifier.Properties.Resources.TrayIcon22,
                ContextMenu = contextMenu,
                Text = "Notifier stays hidden when minimized - click to open", // max 64 chars
                Visible = false
            };

            trayIcon.Click += new System.EventHandler(TrayIcon_Click);
        }

        private void MenuShow_Click(object Sender, EventArgs e)
        {
            window.RestoreWindowState();
        }


        private void MenuClose_Click(object Sender, EventArgs e)
        {
            // Dispose and close the window which exits the app
            Dispose();
            window.Close();
        }

        private void MenuConsole_Click(object Sender, EventArgs e)
        {
            Process.Start(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WFN.exe"));
        }


        public void TrayIcon_Click(object Sender, EventArgs e)
        {
            if (window.WindowState == WindowState.Minimized)
            {
                window.RestoreWindowState();
            }
            else
            {
                window.HideWindowState();
            }
        }

        public void Show()
        {
            trayIcon.Icon = Notifier.Properties.Resources.TrayIcon22;
            trayIcon.Visible = true;
        }
        public void Hide()
        {
            trayIcon.Visible = false;
        }

        public void ShowActivity(string tooltipText)
        {
            tooltipText = tooltipText ?? "Notifier";
            if (tooltipText.Length > 64)
            {
                // limited to 64 chars in Win10
                tooltipText = tooltipText.Substring(0, 63);
            }
            if (trayIcon.Visible)
            {
                trayIcon.Icon = Notifier.Properties.Resources.TrayIcon21;
                ShowBalloonTip(tooltipText);
            }
        }

        private void ShowBalloonTip(string tooltipText)
        {
            if (!activityTipShown)
            {
                activityTipShown = true;
                trayIcon.BalloonTipTitle = "WFN Notifier";
                trayIcon.BalloonTipText = tooltipText;
                trayIcon.BalloonTipIcon = System.Windows.Forms.ToolTipIcon.Warning;
                trayIcon.ShowBalloonTip(400);
            }
        }

        public void Dispose()
        {
            if (components != null)
                components.Dispose();
        }
    }

}
