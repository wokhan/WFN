using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;

namespace Wokhan.WindowsFirewallNotifier.Console.Helpers.ViewModels
{
    public class MonitoredConnectionViewModel : INotifyPropertyChanged
    {
        public string Name { get; set; }

        private int _connectionsCount;
        public int ConnectionsCount
        {
            get { return _connectionsCount; }
            set { _connectionsCount = value; NotifyPropertyChanged("ConnectionsCount"); }
        }

        public string LastIn { get { return CommonHelper.FormatBytes(PointsIn.Last().Y); } }
        public string LastOut { get { return CommonHelper.FormatBytes(PointsOut.Last().Y); } }

        private ObservableCollection<Point> _pointsOut = new ObservableCollection<Point>();
        public ObservableCollection<Point> PointsOut { get { return _pointsOut; } }

        private ObservableCollection<Point> _pointsIn = new ObservableCollection<Point>();
        public ObservableCollection<Point> PointsIn { get { return _pointsIn; } }
        public SolidColorBrush Brush { get; set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }
            set { _isSelected = value; NotifyPropertyChanged("IsSelected"); }
        }

        private bool _isDead;
        public bool IsDead
        {
            get { return _isDead; }
            set { _isDead = value; NotifyPropertyChanged("IsDead"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public MonitoredConnectionViewModel()
        {
            _pointsOut.CollectionChanged += (o, e) => { NotifyPropertyChanged("PointsOut"); NotifyPropertyChanged("LastOut"); };
            _pointsIn.CollectionChanged += (o, e) => { NotifyPropertyChanged("PointsIn"); NotifyPropertyChanged("LastIn"); };
        }

        private void NotifyPropertyChanged(string caller)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(caller));
            }
        }
    }
}
