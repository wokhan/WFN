using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Wokhan.WindowsFirewallNotifier.Notifier.UI.Events;
using Wokhan.WindowsFirewallNotifier.Notifier.Helpers;

namespace Notifier.Tests
{
    [TestClass]
    public class StaticActivityPublisherTest
    {
        internal class TestPublisher : IActivitySender
        {
            public string Name { get; set; }
            internal TestPublisher(string name)
            {
                Name = name;
            }
        }

        [TestMethod]
        public void TestMultiPublish()
        {
            StaticActivityPublisher.ActivityEvent += (sender, args) =>
            {
                Console.WriteLine($"-> event: Sender: {sender.Name}, Activity: {args.Activity}, Timestamp: {args.TimeStamp.ToString("hh:mm:ss.fff")}");
            };

            StaticActivityPublisher.Subscribe( (sender, args) =>
            {
                Console.WriteLine($"-> subscribe: {sender.Name}, {args.Activity},Timestamp: {args.TimeStamp.ToString("hh:mm:ss.fff")}");
            });

            List<Task> tasks = new List<Task>();
            tasks.Add(createTask(new TestPublisher("p1"), ActivityEventArgs.ActivityEnum.Allow, 100));
            tasks.Add(createTask(new TestPublisher("p2"), ActivityEventArgs.ActivityEnum.Block, 199));
            tasks.Add(createTask(new TestPublisher("p3"), ActivityEventArgs.ActivityEnum.Block, 0));
            tasks.ForEach(t =>
            {
                t.Start();
            });
            Task.WaitAll(tasks.ToArray());
        }

        private Task createTask(TestPublisher p, ActivityEventArgs.ActivityEnum activity, int waitMillis)
        {
            void action()
            {
                for (int i = 0; i < 10; i++)
                {
                    Console.WriteLine($"Publish Task: Publisher: {p.Name}, Timestamp: {DateTime.Now.ToString("hh:mm:ss.fff")}");
                    StaticActivityPublisher.Publish(p, activity, new CurrentConn());
                    if (waitMillis == 0)
                    {
                        waitMillis = new Random().Next(10, 200);
                    }
                    Task.Delay(waitMillis);
                }
            }
            return new Task(action);
        }

    }
}
