using Microsoft.VisualBasic.ApplicationServices;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using Wokhan.WindowsFirewallNotifier.Notifier.Helpers;
using Wokhan.WindowsFirewallNotifier.Notifier.UI.Windows;

namespace Wokhan.WindowsFirewallNotifier.Notifier.Managers
{
    class SingletonManager : WindowsFormsApplicationBase, IDisposable
    {
        private App application;

        public SingletonManager()
        {
            IsSingleInstance = true;
            EnableVisualStyles = false;
        }

        protected override bool OnStartup(Microsoft.VisualBasic.ApplicationServices.StartupEventArgs e)
        {
            application = new App(e.CommandLine);
            application.Run();

            return false;
        }

        protected override void OnStartupNextInstance(StartupNextInstanceEventArgs e)
        {
            base.OnStartupNextInstance(e);

            //Give focus to the main instance
            e.BringToForeground = true;

            application.NextInstance(e.CommandLine);
        }

        public void Dispose()
        {
            
        }
    }
}
