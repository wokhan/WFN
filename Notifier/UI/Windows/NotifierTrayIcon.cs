using System;
using System.Windows;
using System.IO;
using WinForms = System.Windows.Forms;
using Messages = Wokhan.WindowsFirewallNotifier.Common.Properties.Resources; // ns for message resources
using System.Diagnostics;
using System.Windows.Forms;
using System.ComponentModel;

namespace Wokhan.WindowsFirewallNotifier.Notifier.UI.Windows
{
    /**
     * Notifier tray icon shown when minimizing the Notifier Window.
     * @Author: harrwiss
     */
    public sealed class NotifierTrayIcon : IDisposable
    {
        private readonly System.Windows.Forms.NotifyIcon trayIcon;
        private readonly IContainer components;

        private bool activityTipShown = false;
        private readonly NotificationWindow notifierWindow;

        public static NotifierTrayIcon Init(NotificationWindow window)
        {
            NotifierTrayIcon factory = new NotifierTrayIcon(window);
            return factory;
        }

        private NotifierTrayIcon() { }
        private NotifierTrayIcon(NotificationWindow notifierWindow)
        {
            //  https://docs.microsoft.com/en-us/dotnet/api/system.windows.forms.notifyicon?redirectedfrom=MSDN&view=netframework-4.8
            this.notifierWindow = notifierWindow;
            components = new System.ComponentModel.Container();

            // Create the NotifyIcon. 
            trayIcon = new WinForms::NotifyIcon(components)
            {
                Icon = Notifier.Properties.Resources.TrayIcon22,
                ContextMenuStrip = InitMenu(),
                Text = Messages.NotifierTrayIcon_NotifierStaysHiddenWhenMinimizedClickToOpen, // max 64 chars
                Visible = false
            };

            trayIcon.Click += new System.EventHandler(TrayIcon_Click);

            notifierWindow.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(notifierWindow.NbConnectionsAfter))
                {
                    ShowActivity();
                }
            };

        }
        void MenuShow_Click(object Sender, EventArgs e)
        {
            notifierWindow.RestoreWindowState();
        }

        void MenuClose_Click(object Sender, EventArgs e)
        {
            // Dispose and close the window which exits the app
            notifierWindow.Close();
            Environment.Exit(0);
        }

        void MenuConsole_Click(object Sender, EventArgs e)
        {
            Process.Start(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WFN.exe"));
        }

        void MenuShowActivity_Click(object Sender, EventArgs e)
        {
            App.GetActivityWindow().Show();
        }

        private WinForms::ContextMenuStrip InitMenu()
        {
            var contextMenu = new WinForms::ContextMenuStrip();
            contextMenu.Items.Add(Messages.NotifierTrayIcon_ShowNotifier, null, MenuShow_Click);
            contextMenu.Items.Add(Messages.NotifierTrayIcon_OpenConsole, null, MenuConsole_Click);
            contextMenu.Items.Add(Messages.NotifierTrayIcon_ShowActivityWindow, null, MenuShowActivity_Click);
            contextMenu.Items.Add(Messages.NotifierTrayIcon_DiscardAndClose, null, MenuClose_Click);
            return contextMenu;
        }

        private void TrayIcon_Click(object Sender, EventArgs e)
        {
            if (WinForms::MouseButtons.Left.Equals(((WinForms::MouseEventArgs)e).Button))
            {
                if (notifierWindow.WindowState == WindowState.Minimized)
                {
                    notifierWindow.RestoreWindowState();
                }
                else
                {
                    notifierWindow.HideWindowState();
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

        public void ShowActivity()
        {
            string tooltipText = Messages.NotifierTrayIcon_ShowActivity_Notifier;

            // TODO: @harrwiss, why using a local method here also?
            void ShowBalloonTip()
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

            // TODO: @harrwiss, tooltipText is a constant, so it's size is already known.
            if (tooltipText.Length > 64)
            {
                // limited to 64 chars in Win10
                tooltipText = tooltipText.Substring(0, 63);
            }
            if (trayIcon.Visible)
            {
                trayIcon.Icon = Notifier.Properties.Resources.TrayIcon21; // with exclamation mark
                ShowBalloonTip();
            }
        }

        public void Dispose()
        {
            trayIcon?.Dispose();
            components?.Dispose();
        }
    }

}
