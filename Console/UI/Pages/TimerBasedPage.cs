using CommunityToolkit.Mvvm.ComponentModel;

using System.Timers;
using System.Windows;
using System.Windows.Controls;

namespace Wokhan.WindowsFirewallNotifier.Console.UI.Pages;

[ObservableObject]
public partial class TimerBasedPage : Page
{
    private readonly System.Timers.Timer timer;

    public virtual List<double> Intervals { get; } = [0.5, 1, 5, 10];

    public virtual bool IsTrackingEnabled
    {
        get => timer.Enabled;
        set
        {
            if (timer.Enabled != value)
            {
                timer.Enabled = value;
                OnPropertyChanged();
            }
        }
    }


    private bool? wasRunningWhenUnloaded;
    private bool isCurrentlyRunning;

    private double _interval = 1;
    public virtual double Interval
    {
        get { return _interval; }
        set { _interval = value; timer.Interval = Interval * 1000; }
    }

    public TimerBasedPage()
    {
        this.Loaded += Page_Loaded;
        this.Unloaded += Page_Unloaded;

        timer = new(Interval * 1000);
        timer.Elapsed += Timer_Tick;
    }

    private void Timer_Tick(object? sender, ElapsedEventArgs e)
    {
        if (!isCurrentlyRunning)
        {
            isCurrentlyRunning = true;
            OnTimerTick(sender, e);
            isCurrentlyRunning = false;
        }
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        IsTrackingEnabled = wasRunningWhenUnloaded ?? true;
    }

    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        wasRunningWhenUnloaded = IsTrackingEnabled;
        IsTrackingEnabled = false;
    }

#pragma warning disable CS1998 // Async warning suppression
    protected virtual void OnTimerTick(object? sender, ElapsedEventArgs e) { }
    protected virtual void OnTimerStart() { }
    protected virtual void OnTimerStop() { }
#pragma warning restore CS1998 // Async warning suppression
}
