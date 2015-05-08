using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Wokhan.WindowsFirewallNotifier.Console.Helpers.BindingConverters
{
    [ValueConversion(typeof(ObservableCollection<Point>), typeof(PointCollection))]
    public class ObservablePointCollectionConverter : IValueConverter
    {
        private Dictionary<ObservableCollection<Point>, PointCollection> pointMap = new Dictionary<ObservableCollection<Point>, PointCollection>();

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {

            var src = (ObservableCollection<Point>)value;
            return new PointCollection(src);

            PointCollection ret;
            if (pointMap.TryGetValue(src, out ret))
            {
                return ret;
            }
            else
            {
                ret = new PointCollection(src);
                pointMap.Add(src, ret);
                src.CollectionChanged += ObservablePointCollectionConverter_CollectionChanged;
            }

            return ret;
        }

        void ObservablePointCollectionConverter_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            PointCollection points = pointMap[(ObservableCollection<Point>)sender];

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    for (int i = 0; i < e.NewItems.Count; i++)
                    {
                        points.Insert(e.NewStartingIndex + i, (Point)e.NewItems[i]);
                    }
                    break;

                case NotifyCollectionChangedAction.Move:
                    for (int i = 0; i < e.NewItems.Count; i++)
                    {
                        points.RemoveAt(e.OldStartingIndex);
                        points.Insert(e.NewStartingIndex + i, (Point)e.NewItems[i]);
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    for (int i = 0; i < e.OldItems.Count; i++)
                    {
                        points.RemoveAt(e.OldStartingIndex);
                    }
                    break;

                case NotifyCollectionChangedAction.Replace:
                    for (int i = 0; i < e.NewItems.Count; i++)
                    {
                        points[e.NewStartingIndex + i] = (Point)e.NewItems[i];
                    }
                    break;

                case NotifyCollectionChangedAction.Reset:
                    points.Clear();
                    break;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}
