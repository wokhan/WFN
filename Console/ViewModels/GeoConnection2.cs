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
using Wokhan.WindowsFirewallNotifier.Common.Core;
using System.Runtime.CompilerServices;

namespace Wokhan.WindowsFirewallNotifier.Console.ViewModels
{
    //TODO: Inherit from Connection as well
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

        //TODO: CurrentIP shouldn't be handled in GeoConnection2 class (but in IPHelper or something alike)
        private static IPAddress _currentIP;
        public static IPAddress CurrentIP => (_currentIP ?? (_currentIP = IPHelper.GetPublicIpAddress()));

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
        public Location Coordinates => _coordinates ?? (_coordinates = IPToLocation(Connection.TargetIP));

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

        private LocationCollection _rayCoordinates;
        public LocationCollection RayCoordinates => _rayCoordinates ?? (_rayCoordinates = new LocationCollection() { CurrentCoordinates, Coordinates });

        //TODO: should be defined on the Map side, not GeoConnection, as it relies on Bing Maps control
        private LocationCollection _fullRoute;
        public LocationCollection FullRoute => this.GetOrSetValueAsync(ComputeRoute, NotifyPropertyChanged, nameof(_fullRoute));

        public static bool Initialized { get; private set; }

        private async Task<LocationCollection> ComputeRoute()
        {
            var collection = new LocationCollection { CurrentCoordinates };
            var fullroute = await IPHelper.GetFullRoute(Connection.TargetIP).ConfigureAwait(false);
            foreach (var location in fullroute.Select(ip => IPToLocation(ip)).Where(l => l.Latitude != 0 && l.Longitude != 0))
            {
                collection.Add(location);
            }
            return collection;
        }

        private static readonly string _DB_PATH = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\IPDatabase\GeoLite2-City.mmdb");
        public static bool CheckDB()
        {
            return File.Exists(_DB_PATH);
        }

        private static DatabaseReader _databaseReader;
        private static bool initPending;

        // Indicates whether the actual location has been injected
        private bool startingPointAddedSingle;
        private bool startingPointAddedFull;

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

        internal void UpdateStartingPoint(Location currentCoordinates)
        {
            if (startingPointAddedSingle)
            {
                this.RayCoordinates.RemoveAt(0);
            }
            startingPointAddedSingle = true;
            this.RayCoordinates.Insert(0, currentCoordinates);
            NotifyPropertyChanged(nameof(RayCoordinates));

            if (startingPointAddedFull)
            {
                this.FullRoute.RemoveAt(0);
            }

            if (this.FullRoute != null)
            {
                startingPointAddedFull = true;
                this.FullRoute.Insert(0, currentCoordinates);
                NotifyPropertyChanged(nameof(FullRoute));
            }
        }

        protected void NotifyPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}