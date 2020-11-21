using Microsoft.Maps.MapControl.WPF;
using System.Linq;
using System.IO;
using System;
using System.Net;
using System.Threading.Tasks;
using MaxMind.GeoIP2;
using MaxMind.GeoIP2.Responses;
using Resources = Wokhan.WindowsFirewallNotifier.Common.Properties.Resources;
using Wokhan.WindowsFirewallNotifier.Common.Net.IP;
using System.ComponentModel;

namespace Wokhan.WindowsFirewallNotifier.Console.ViewModels
{
    /// <summary>
    /// Geo locations v2 using GeoIP2 API supporting ipv4 andv ipv6.<para>
    /// Uses database from: https://dev.maxmind.com/geoip/geoip2/geolite2/
    /// GeoIP2 API: https://github.com/maxmind/GeoIP2-dotnet
    /// </para>
    /// </summary>
    public class GeoConnection2 : INotifyPropertyChanged
    {
        public Connection Connection { get; private set; }

        public string Owner => Connection.Owner;

        private static IPAddress _currentIP;
        //TODO: CurrentIP shouldn't be handled in GeoConnection2 class (but in IPHelper or something alike)
        public static IPAddress CurrentIP
        {
            get
            {
                if (_currentIP is null)
                {
                    {
                        _currentIP = IPHelper.GetPublicIpAddress();
                    }
                }

                return _currentIP;
            }
        }

        private static Location _currentCoordinates;
        public static Location CurrentCoordinates
        {
            get
            {
                if (_currentCoordinates is null)
                {
                    if (CurrentIP == IPAddress.None)
                    {
                        throw new Exception(Resources.GeoConnection2_CannotRetrieveConnectionLocationForPublicIp);
                    }
                    _currentCoordinates = IPToLocation(CurrentIP);
                }
                return _currentCoordinates;
            }
        }

        private Location _coordinates;
        public Location Coordinates
        {
            get
            {
                if (_coordinates is null)
                {
                    _coordinates = IPToLocation(Connection.TargetIP);
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

            return new Location();
        }

        private static Location IPToLocation(IPAddress address)
        {
            if (_databaseReader.TryCity(address, out CityResponse cr))
            {
                return new Location(cr.Location.Latitude.GetValueOrDefault(), cr.Location.Longitude.GetValueOrDefault());
            }
            return new Location();
        }
        public GeoConnection2(Connection ownerMod)
        {
            Connection = ownerMod;
        }

        public LocationCollection RayCoordinates
        {
            get
            {
                return new LocationCollection() { CurrentCoordinates, Coordinates };
            }
        }

        private bool computePending;
        private LocationCollection _fullRoute;
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

        public static bool Initialized { get; private set; }

        private async void ComputeRoute()
        {
            var r = new LocationCollection
            {
                CurrentCoordinates
            };
            foreach (var x in (await IPHelper.GetFullRoute(Connection.TargetIP).ConfigureAwait(false)).Select(ip => IPToLocation(ip)).Where(l => l.Latitude != 0 && l.Longitude != 0))
            {
                r.Add(x);
            }

            _fullRoute = r;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FullRoute)));
        }

        private static readonly string _DB_PATH = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\IPDatabase\GeoLite2-City.mmdb");
        public static bool CheckDB()
        {
            return File.Exists(_DB_PATH);
        }

        private static DatabaseReader _databaseReader;
        private static bool initPending;

        public event PropertyChangedEventHandler PropertyChanged;

        public static async Task<bool> InitCache()
        {
            if (initPending)
            {
                return true;
            }

            return await Task.Run(() =>
                {
                    initPending = true;
                    if (_databaseReader is null)
                    {
                        _databaseReader = new DatabaseReader(_DB_PATH);
                    }
                    initPending = false;
                    Initialized = true;
                    return true;
                }).ConfigureAwait(false);
        }
    }
}