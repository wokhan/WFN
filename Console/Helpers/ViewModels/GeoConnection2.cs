using Microsoft.Maps.MapControl.WPF;
using System.Linq;
using System.IO;
using System;
using System.Net;
using System.Windows.Media;
using System.Threading.Tasks;
using MaxMind.GeoIP2;
using MaxMind.GeoIP2.Responses;
using Resources = Wokhan.WindowsFirewallNotifier.Common.Properties.Resources;
using Wokhan.WindowsFirewallNotifier.Common.Net.IP;

namespace Wokhan.WindowsFirewallNotifier.Console.Helpers.ViewModels
{
    /// <summary>
    /// Geo locations v2 using GeoIP2 API supporting ipv4 andv ipv6.<para>
    /// Uses database from: https://dev.maxmind.com/geoip/geoip2/geolite2/
    /// GeoIP2 API: https://github.com/maxmind/GeoIP2-dotnet
    /// </para>
    /// </summary>
    public class GeoConnection2 : Connection
    {
        public Brush Brush { get; set; }

        private static Location _currentCoordinates = null;
        public static Location CurrentCoordinates
        {
            get
            {
                if (_currentCoordinates is null)
                {
                    IPAddress address = IPHelper.GetPublicIpAddress();
                    if (address == IPAddress.None)
                    {
                        throw new Exception(Resources.GeoConnection2_CannotRetrieveConnectionLocationForPublicIp);
                    }
                    _currentCoordinates = IPToLocation(address);
                }
                return _currentCoordinates;
            }
        }

        private Location _coordinates = null;
        public Location Coordinates
        {
            get
            {
                if (_coordinates is null)
                {
                    _coordinates = IPToLocation(this.RemoteAddress);
                }

                return _coordinates;
            }
        }

        /// <summary>
        /// Retrieves the location based on IPv4 or IPv6 address.
        /// </summary>
        /// <param name="address">Standard IPv4 or IPv6 address</param>
        /// <returns></returns>
        private static Location IPToLocation(string address)
        {
            if (IPAddress.TryParse(address, out IPAddress adr))
            {
                return IPToLocation(adr);
            }

            return null;
        }

        private static Location IPToLocation(IPAddress address)
        {
            if (_databaseReader.TryCity(address, out CityResponse cr))
            {
                return new Location(cr.Location.Latitude.GetValueOrDefault(), cr.Location.Longitude.GetValueOrDefault());
            }
            return new Location();
        }
        public GeoConnection2(IConnectionOwnerInfo ownerMod) : base(ownerMod)
        {
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
                if (_fullRoute is null && !computePending)
                {
                    computePending = true;
                    ComputeRoute();
                }

                return _fullRoute;
            }
        }


        private async void ComputeRoute()
        {
            var r = new LocationCollection
            {
                CurrentCoordinates
            };
            foreach (var x in (await IPHelper.GetFullRoute(RemoteAddress).ConfigureAwait(false)).Select(ip => IPToLocation(ip)).Where(l => l.Latitude != 0 && l.Longitude != 0))
            {
                r.Add(x);
            }

            _fullRoute = r;

            NotifyPropertyChanged(nameof(FullRoute));
        }

        private static readonly string _DB_PATH = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"IPDatabase\GeoLite2-City.mmdb");
        public static bool CheckDB()
        {
            return File.Exists(_DB_PATH);
        }

        private static DatabaseReader _databaseReader;
        public static async Task<bool> InitCache()
        {
            return await Task.Run(() =>
                {
                    if (_databaseReader is null)
                    {
                        _databaseReader = InitDatabaseReader(_DB_PATH);
                    }
                    return true;
                }).ConfigureAwait(false);
        }

        /// <summary>
        /// This creates the DatabaseReader object, which should be reused across lookups.
        /// </summary>
        /// <param name="dbPath"></param>
        /// <returns></returns>
        public static DatabaseReader InitDatabaseReader(string dbPath = null)
        {
            dbPath = string.IsNullOrEmpty(dbPath) ? _DB_PATH : dbPath;
            DatabaseReader reader = new DatabaseReader(dbPath);
            return reader;
        }
    }
}