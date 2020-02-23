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
using WinForms = System.Windows.Forms;
using Messages = Wokhan.WindowsFirewallNotifier.Common.Properties.Resources; // ns for message resources
using System.Diagnostics;

namespace Wokhan.WindowsFirewallNotifier.Notifier.UI.Windows
{
    /**
     * Notifier tray icon shown when minimizing the Notifier Window.
     * @Author: harrwiss
     */
    public class NotifierTrayIcon
    {
        private readonly System.Windows.Forms.NotifyIcon trayIcon;
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

            // Create the NotifyIcon. 
            trayIcon = new WinForms::NotifyIcon(components)
            {
                Icon = Notifier.Properties.Resources.TrayIcon22,
                ContextMenu = initMenu(),
                Text = Messages.NotifierTrayIcon_NotifierStaysHiddenWhenMinimizedClickToOpen, // max 64 chars
                Visible = false
            };

            trayIcon.Click += new System.EventHandler(TrayIcon_Click);
        }

        private WinForms::ContextMenu initMenu()
        {
            WinForms::ContextMenu contextMenu = new WinForms::ContextMenu();
            void MenuShow_Click(object Sender, EventArgs e)
            {
                window.RestoreWindowState();
            }

            void MenuClose_Click(object Sender, EventArgs e)
            {
                // Dispose and close the window which exits the app
                window.Close();
                contextMenu.Dispose();
                trayIcon.Dispose();
                Environment.Exit(0);
            }

            void MenuConsole_Click(object Sender, EventArgs e)
            {
                Process.Start(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WFN.exe"));
            }
            contextMenu.MenuItems.Add(Messages.NotifierTrayIcon_ShowNotifier, MenuShow_Click);
            contextMenu.MenuItems.Add(Messages.NotifierTrayIcon_OpenConsole, MenuConsole_Click);
            contextMenu.MenuItems.Add(Messages.NotifierTrayIcon_DiscardAndClose, MenuClose_Click);

            return contextMenu;
        }

        private void TrayIcon_Click(object Sender, EventArgs e)
        {
            if (WinForms::MouseButtons.Left.Equals(((WinForms::MouseEventArgs)e).Button)) {
                if (window.WindowState == WindowState.Minimized)
                {
                    window.RestoreWindowState();
                }
                else
                {
                    window.HideWindowState();
                }
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
            tooltipText = tooltipText ?? Messages.NotifierTrayIcon_ShowActivity_Notifier;
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
                trayIcon.BalloonTipTitle = Messages.NotifierTrayIcon_ShowBalloonTip_WFNNotifier;
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
