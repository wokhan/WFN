using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Wokhan.WindowsFirewallNotifier.Console.UI.Pages
{
    public class TimerBasedPage : Page, INotifyPropertyChanged
    {
        private DispatcherTimer timer = new DispatcherTimer();

        public virtual List<double> Intervals { get; } = new List<double> { 0.5, 1, 5, 10 };

        public virtual bool IsTrackingEnabled
        {
            get { return timer.IsEnabled; }
            set { timer.IsEnabled = value; NotifyPropertyChanged(); }
        }

        private double _interval = 1;

        public event PropertyChangedEventHandler PropertyChanged;

        private bool? wasRunningWhenUnloaded;
        private bool isCurrentlyRunning;

        public virtual double Interval
        {
            get { return _interval; }
            set { _interval = value; timer.Interval = TimeSpan.FromSeconds(value); }
        }

        public TimerBasedPage()
        {
            this.Loaded += Page_Loaded;
            this.Unloaded += Page_Unloaded;

            timer.Interval = TimeSpan.FromSeconds(Interval);
            timer.Tick += Timer_Tick; 
        }

        private async void Timer_Tick(object sender, EventArgs e)
        {
            if (!isCurrentlyRunning)
            {
                isCurrentlyRunning = true;
                await OnTimerTick(sender, e).ConfigureAwait(false);
            }

            isCurrentlyRunning = false;
        }

        protected void NotifyPropertyChanged([CallerMemberName] string caller = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(caller));
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (wasRunningWhenUnloaded.HasValue)
            {
                IsTrackingEnabled = (bool)wasRunningWhenUnloaded;
            }
            else
            {
                IsTrackingEnabled = true;
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            wasRunningWhenUnloaded = IsTrackingEnabled;
            IsTrackingEnabled = false;
        }

#pragma warning disable CS1998 // Async warning suppression
        protected virtual async Task OnTimerTick(object sender, EventArgs e) { }
        protected virtual void OnTimerStart() { }
        protected virtual void OnTimerStop() { }
#pragma warning restore CS1998 // Async warning suppression
    }
}
