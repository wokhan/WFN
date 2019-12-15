using Microsoft.VisualBasic.ApplicationServices;
using System;

namespace Wokhan.WindowsFirewallNotifier.Notifier.Managers
{
    /// <summary>
    /// Assures that the program is only stated once.
    /// </summary>
    class SingletonManager : WindowsFormsApplicationBase
    {
        private static App application;

        public SingletonManager()
        {
            IsSingleInstance = true;
            EnableVisualStyles = false;
        }

        protected override bool OnStartup(StartupEventArgs e)
        {
            application = new App();
            application.Run();

            return false;
        }

        protected override void OnStartupNextInstance(StartupNextInstanceEventArgs e)
        {
            //Give focus to the main instance
            e.BringToForeground = true;
            base.OnStartupNextInstance(e);
        }
    }
}
