using Wokhan.WindowsFirewallNotifier.Common.Helpers;
using Microsoft.Maps.MapControl.WPF;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Globalization;
using System;
using System.Net;
using System.Windows.Media;
using System.Net.NetworkInformation;
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
                    _currentCoordinates = IPToLocation(GetPublicIpAddress());
                }
                return _currentCoordinates;
            }
        }


        private static async Task<IEnumerable<IPAddress>> GetFullRoute(string adr)
        {
            Ping pong = new Ping();
            PingOptions po = new PingOptions(1, true);
            List<IPAddress> ret = new List<IPAddress>();
            PingReply r = null;
            for (int i = 1; i < 30; i++)
            {
                if (r != null && r.Status != IPStatus.TimedOut)
                {
                    po.Ttl = i;
                }
                r = await pong.SendPingAsync(adr, 4000, new byte[32], po);
                
                if (r.Status == IPStatus.TtlExpired)
                {
                    ret.Add(r.Address);
                }
                else
                {
                    break;
                }
            }
            ret.Add(IPAddress.Parse(adr));
            return ret;
        }

        //private static List<IPAddress> defaultGateway = NetworkInterface.GetAllNetworkInterfaces().Where(i => i.OperationalStatus == OperationalStatus.Up).Select(i => i.GetIPProperties().GatewayAddresses.First().Address).ToList();
        private static IPAddress GetPublicIpAddress()
        {
            //Ping pong = new Ping();
            //PingOptions po = new PingOptions(1, true);
            //IPAddress ret;
            //bool next;
            //for (int i = 1; i < 30; i++)
            //{
            //    po.Ttl = i;
            //    var r = pong.Send("www.microsoft.com", 4000, new byte[1], po);
            //    if (r.Status == IPStatus.TtlExpired && defaultGateway.Contains(r.Address))
            //    {
            //        return r.Address;
            //    }
            //}

            //return null;
            currentAddress = IPAddress.Parse("92.144.89.116");
            if (currentAddress == null)
            {
                var request = (HttpWebRequest)WebRequest.Create("http://ifconfig.me/ip");
                request.Method = "GET";
                request.UserAgent = "curl";
                try
                {
                    using (WebResponse response = request.GetResponse())
                    {
                        using (var reader = new StreamReader(response.GetResponseStream()))
                        {
                            currentAddress = IPAddress.Parse(reader.ReadToEnd().Replace("\n", ""));
                        }
                    }
                }
                catch
                {
                    return null;
                }
            }

            return currentAddress;
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

            if (addrBytes.Length > 8)
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
            foreach (var x in (await GetFullRoute(this.RemoteAddress)).Select(ip => IPToLocation(ip)).Where(l => l.Latitude != 0 && l.Longitude != 0))
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

        public static async Task<bool> InitCache()
        {
            return await Task.Run(() =>
            {
                using (var file = new FileStream("D:\\Downloads\\IPDatabase.gz", FileMode.Open))
                {
                    /*var x = new FileStream("D:\\Downloads\\IPDatabase.gz", FileMode.CreateNew);

                    using (var gzc = new GZipStream(x, CompressionLevel.Optimal, false))
                    {
                        file.CopyTo(gzc);
                    }
                    */
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