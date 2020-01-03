using System;
using System.ComponentModel;
using System.Windows.Media;

namespace Wokhan.WindowsFirewallNotifier.Console.Helpers.ViewModels
{
    public class GroupedViewBase : INotifyPropertyChanged
    {
        private Brush _brush;
        public Brush Brush
        {
            get { return _brush; }
            set { _brush = value; NotifyPropertyChanged(nameof(Brush)); }
        }

        public string Name { get; set; }
        private int _count;
        public int Count
        {
            get { return _count; }
            set { _count = value; NotifyPropertyChanged(nameof(Count)); }
        }


        private bool _isAccessDenied;
        public bool IsAccessDenied
        {
            get { return _isAccessDenied; }
            set { _isAccessDenied = value; NotifyPropertyChanged(nameof(IsAccessDenied)); }
        }

        private string _lastError;
        public string LastError
        {
            get { return _lastError; }
            set { _lastError = value; NotifyPropertyChanged(nameof(LastError)); }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }
            set { _isSelected = value; NotifyPropertyChanged(nameof(IsSelected)); }
        }

        public ImageSource Icon { get; set; }

        public DateTime LastSeen { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(string caller)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(caller));
            }
        }
    }
}