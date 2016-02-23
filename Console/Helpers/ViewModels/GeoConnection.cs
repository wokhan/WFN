using Wokhan.WindowsFirewallNotifier.Common.Helpers;
using Microsoft.Maps.MapControl.WPF;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Globalization;
using System;
using System.Net;
using System.Windows.Media;
using System.Threading.Tasks;
using System.IO.Compression;
using Wokhan.WindowsFirewallNotifier.Common.Extensions;

namespace Wokhan.WindowsFirewallNotifier.Console.Helpers.ViewModels
{
    public class GeoConnection : Connection
    {
        public Brush Brush { get; set; }

        private static IPAddress currentAddress = null;

        private static Location _currentCoordinates = null;
        public static Location CurrentCoordinates
        {
            get
            {
                if (_currentCoordinates == null)
                {
                    _currentCoordinates = IPToLocation(IPHelper.GetPublicIpAddress());
                }
                return _currentCoordinates;
            }
        }
                
        private Location _coordinates = null;
        public Location Coordinates
        {
            get
            {
                if (_coordinates == null)
                {
                    _coordinates = IPToLocation(this.RemoteAddress);
                }

                return _coordinates;
            }
        }

        private static Location IPToLocation(string address)
        {
            IPAddress adr;
            if (IPAddress.TryParse(address, out adr))
            {
                return IPToLocation(adr);
            }

            return null;
        }

        private static Location IPToLocation(IPAddress address)
        {
            ulong ipnum;

            byte[] addrBytes = address.GetAddressBytes();

            if (BitConverter.IsLittleEndian)
            {
                addrBytes = addrBytes.Reverse().ToArray();
            }

            if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                //IPv6
                ipnum = BitConverter.ToUInt64(addrBytes, 8);
                ipnum <<= 64;
                ipnum += BitConverter.ToUInt64(addrBytes, 0);
            }
            else
            {
                //IPv4
                ipnum = BitConverter.ToUInt32(addrBytes, 0);
            }

            if (allCoords != null)
            {
                var m = allCoords.AsParallel().FirstOrDefault(c => ipnum >= c.Start && ipnum < c.End);
                //if (m != null)
                {
                    return new Location(m.Latitude, m.Longitude);
                }
                /* else
                 {
                     return new Location();
                 }*/
            }
            else
            {
                return null;
            }
        }

        public LocationCollection RayCoordinates
        {
            get
            {
                return new LocationCollection() { CurrentCoordinates, Coordinates };
            }
        }

        private bool computePending;
        private LocationCollection _fullRoute = null;
        public LocationCollection FullRoute
        {
            get
            {
                if (_fullRoute == null && !computePending)
                {
                    computePending = true;
                    ComputeRoute();
                }

                return _fullRoute;
            }
        }
    

        private async void ComputeRoute()
        {
            var r = new LocationCollection();
            r.Add(CurrentCoordinates);
            foreach (var x in (await IPHelper.GetFullRoute(this.RemoteAddress)).Select(ip => IPToLocation(ip)).Where(l => l.Latitude != 0 && l.Longitude != 0))
            {
                r.Add(x);
            }

            _fullRoute = r;

            NotifyPropertyChanged("FullRoute");
        }

        public struct Coords
        {
            public uint Start;
            public uint End;
            public double Longitude;
            public double Latitude;
        }

        private static List<Coords> allCoords = new List<Coords>(1956497);

        public GeoConnection(IPHelper.I_OWNER_MODULE ownerMod) : base(ownerMod)
        {
        }

        public static bool CheckDB()
        {
            return File.Exists(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "IPDatabase.gz"));
        }

        public static async Task<bool> InitCache()
        {
            return await Task.Run(() =>
            {
                using (var file = new FileStream(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "IPDatabase.gz"), FileMode.Open))
                {
                    using (var gz = new GZipStream(file, CompressionMode.Decompress))
                    {
                        using (var sr = new StreamReader(gz))
                        {
                            allCoords = sr.ReadLines()
                                          .Select(l => l.Split(','))
                                          .Select(l => new Coords() { Start = uint.Parse(l[0]), End = uint.Parse(l[1]), Latitude = double.Parse(l[2], CultureInfo.InvariantCulture), Longitude = double.Parse(l[3], CultureInfo.InvariantCulture) })
                                          .ToList();
                        }
                    }
                }
                return true;
            });
        }
    }
}