using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Wokhan.WindowsFirewallNotifier.Console.UI.Controls;

namespace Wokhan.WindowsFirewallNotifier.Console.Helpers.ViewModels
{
    public class GroupedView : GroupedViewBase
    {

        private string _lastin;
        public string LastIn
        {
            get { return _lastin; }
            set { _lastin = value; NotifyPropertyChanged(nameof(LastIn)); }
        }

        private string _lastout;
        public string LastOut
        {
            get { return _lastout; }
            set { _lastout = value; NotifyPropertyChanged(nameof(LastOut)); }
        }
        public LineChart.Series SeriesIn { get; set; }
        public LineChart.Series SeriesOut { get; set; }

    }
}
