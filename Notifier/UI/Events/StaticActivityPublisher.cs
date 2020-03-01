using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wokhan.WindowsFirewallNotifier.Notifier.Helpers;

namespace Wokhan.WindowsFirewallNotifier.Notifier.UI.Events
{
    public interface IActivitySender
    {
        string Name { get; set; }
    }

    /*
     * Static activity event publisher.
     * See:
     * https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/events/how-to-publish-events-that-conform-to-net-framework-guidelines
     */
    public static class StaticActivityPublisher
    {
        public delegate void ActivityEventHandler<TActivityEventArgs>(IActivitySender sender, ActivityEventArgs args);
        
        public static event ActivityEventHandler<ActivityEventArgs> ActivityEvent;

        public static void Publish(IActivitySender sender, ActivityEventArgs.ActivityEnum activity, CurrentConn currentConnection)
        {
            ActivityEventArgs args = new ActivityEventArgs(activity, currentConnection);
            OnRaiseActivityEvent(sender, args);
        }

        public static void Subscribe(ActivityEventHandler<ActivityEventArgs> eventHandler)
        {
            ActivityEvent += eventHandler;
        }

        // Wrap event invocations inside a protected virtual method
        // to allow derived classes to override the event invocation behavior
        private static void OnRaiseActivityEvent(IActivitySender sender, ActivityEventArgs args)
        {
            //// Make a temporary copy of the event to avoid possibility of
            //// a race condition if the last subscriber unsubscribes
            //// immediately after the null check and before the event is raised.
            //EventHandler<ActivityEventArgs> handler = RaiseActivityEvent;

            //// Event will be null if there are no subscribers
            //if (handler != null)
            //{
            //    //// Format the string to send inside the CustomEventArgs parameter
            //    //e.Message += $" at {DateTime.Now}";

            //    // Use the () operator to raise the event.
            //    handler(sender, e);
            //}
            ActivityEvent?.Invoke(sender, args);
        }
    }
}
