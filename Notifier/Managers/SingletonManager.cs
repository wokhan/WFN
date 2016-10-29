using Microsoft.VisualBasic.ApplicationServices;
using System;
using System.Threading;

namespace Wokhan.WindowsFirewallNotifier.Notifier.Managers
{
    class SingletonManager : WindowsFormsApplicationBase, IDisposable
    {
        private App application;

        private Mutex mutex = new Mutex();

        public SingletonManager()
        {
            IsSingleInstance = true;
            EnableVisualStyles = false;
        }

        protected override bool OnStartup(Microsoft.VisualBasic.ApplicationServices.StartupEventArgs e)
        {
            mutex.WaitOne();
            try
            {
                application = new App(e.CommandLine);
                application.Run();
            }
            finally
            {
                mutex.ReleaseMutex();
            }

            return false;
        }

        protected override void OnStartupNextInstance(StartupNextInstanceEventArgs e)
        {
            base.OnStartupNextInstance(e);

            //Give focus to the main instance
            e.BringToForeground = true;

            //There is a race condition where the original program might already have shutdown. Let's try and work around that.
            //Note: Using double-checked locking here.
            if (application == null)
            {
                mutex.WaitOne();
                try
                {
                    if (application == null)
                    {
                        application = new App(e.CommandLine);
                        application.Run();
                        return;
                    }
                }
                finally
                {
                    mutex.ReleaseMutex();
                }
            }
            application.NextInstance(e.CommandLine);
        }

        public void Dispose()
        {
            mutex.Dispose();
        }
    }
}
