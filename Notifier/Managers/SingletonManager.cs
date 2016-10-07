using Microsoft.VisualBasic.ApplicationServices;
using System;

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

            if (application == null)
            {
                //There is a race condition where the original program might already have shutdown. Let's try and work around that.
                application = new App(e.CommandLine);
                application.Run();
                return;
            }
            application.NextInstance(e.CommandLine);
        }

        public void Dispose()
        {
            
        }
    }
}
