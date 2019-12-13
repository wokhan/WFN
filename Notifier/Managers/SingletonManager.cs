using Microsoft.VisualBasic.ApplicationServices;

namespace Wokhan.WindowsFirewallNotifier.Notifier.Managers
{
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
            //application = new App(e.CommandLine);
            //application.Run();

            return false;
        }

        protected override void OnStartupNextInstance(StartupNextInstanceEventArgs e)
        {
            //Give focus to the main instance
            e.BringToForeground = true;

            base.OnStartupNextInstance(e);

            //application.NextInstance(e.CommandLine);
        }
    }
}
