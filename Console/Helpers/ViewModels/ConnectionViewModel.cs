using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Media;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;

namespace Wokhan.WindowsFirewallNotifier.Console.Helpers.ViewModels
{
        public class ConnectionViewModel : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            protected void NotifyPropertyChanged(string propertyName)
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
            }

            public ConnectionViewModel(int pid)
            {
                PID = pid;
                IsNew = true;
                LastSeen = DateTime.Now;

                if (pid != 0 && pid != 4)
                {
                    try
                    {
                        using (Process proc = Process.GetProcessById(pid))
                        {
                            ProcName = proc.ProcessName;
                            try
                            {
                                Path = proc.MainModule.FileName;
                                Icon = ProcessHelper.GetCachedIcon(Path, true);
                            }
                            catch
                            {
                                Path = "Unresolved";
                                Icon = ProcessHelper.GetCachedIcon("?error", true);
                            }
                        }
                    }
                    catch
                    {
                        ProcName = "[Missing process]";
                        Path = "Unknown";
                        Icon = ProcessHelper.GetCachedIcon("?missing", true);
                    }
                }
                else
                {
                    Path = "System";
                    ProcName = "System";
                    Icon = ProcessHelper.GetCachedIcon("?system", true);
                }
            }

            public string GroupKey { get { return String.Format("{0} ({1}) - [{2}]", ProcName, Path, PID); } }
            public ImageSource Icon { get; set; }
            public long PID { get; set; }
            public string ProcName { get; set; }
            public string Path { get; set; }

            private string _protocol;
            public string Protocol
            {
                get { return _protocol; }
                set { _protocol = value; NotifyPropertyChanged("Protocol"); }
            }

            private string _state;
            public string State
            {
                get { return _state; }
                set { _state = value; NotifyPropertyChanged("State"); }
            }

            private string _localAddress;
            public string LocalAddress
            {
                get { return _localAddress; }
                set { _localAddress = value; NotifyPropertyChanged("LocalAddress"); }
            }

            private string _localPort;
            public string LocalPort
            {
                get { return _localPort; }
                set { _localPort = value; NotifyPropertyChanged("LocalPort"); }
            }

            private string _remoteAddress;
            public string RemoteAddress
            {
                get { return _remoteAddress; }
                set { _remoteAddress = value; NotifyPropertyChanged("RemoteAddress"); }
            }

            private string _remotePort;
            public string RemotePort
            {
                get { return _remotePort; }
                set { _remotePort = value; NotifyPropertyChanged("RemotePort"); }
            }

            public string Owner { get; set; }
            public DateTime CreationTime { get; set; }

            private DateTime _lastSeen;
            public DateTime LastSeen
            {
                get { return _lastSeen; }
                set { _lastSeen = value; NotifyPropertyChanged("RemotePort"); }
            }

            private bool _isDying;
            public bool IsDying
            {
                get { return _isDying; }
                set { _isDying = value; NotifyPropertyChanged("IsDying"); }
            }

            private bool _isNew;
            public bool IsNew
            {
                get { return _isNew; }
                set { _isNew = value; NotifyPropertyChanged("IsNew"); }
            }
        }
}
