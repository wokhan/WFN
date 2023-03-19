using MaxMind.GeoIP2;
using MaxMind.GeoIP2.Responses;

using Microsoft.Maps.MapControl.WPF;

using System;
using System.Device.Location;
using System.IO;
using System.Net;
using System.Threading.Tasks;

using Wokhan.WindowsFirewallNotifier.Common.Logging;
using Wokhan.WindowsFirewallNotifier.Common.Net.IP;
using Wokhan.WindowsFirewallNotifier.Common.Properties;

namespace Wokhan.WindowsFirewallNotifier.Console.Helpers;

public static class GeoLocationHelper 
{
    // Should be disposed (and thus not static...)
    private static GeoCoordinateWatcher? geoWatcher;

    public static event EventHandler? CurrentCoordinatesChanged;
    
    private static Location? _currentCoordinates;
    public static Location? CurrentCoordinates
    {
        get => _currentCoordinates;
        private set
        {
            if (_currentCoordinates != value)
            {
                _currentCoordinates = value;
                CurrentCoordinatesChanged?.Invoke(null, EventArgs.Empty);
            }
        }
    }

    private static Location _unknownLocation = new();

    /// <summary>
    /// Retrieves the location based on IPv4 or IPv6 address.
    /// </summary>
    /// <param name="address">Standard IPv4 or IPv6 address</param>
    /// <returns></returns>
    public static async Task<Location> IPToLocationAsync(string? address)
    {
        if (address is not null && IPAddress.TryParse(address, out IPAddress? adr))
        {
            return await IPToLocationAsync(adr);
        }

        return _unknownLocation;
    }

    public static async Task<Location> IPToLocationAsync(IPAddress? address)
    {
        if (address is null)
        {
            return _unknownLocation;
        }

        if (address == IPAddress.None)
        {
            throw new Exception(Resources.GeoConnection2_CannotRetrieveConnectionLocationForPublicIp);
        }

        return await Task.Run(() =>
        {
            if (_databaseReader?.TryCity(address, out CityResponse? cr) ?? false)
            {
                return new Location(cr!.Location.Latitude.GetValueOrDefault(), cr.Location.Longitude.GetValueOrDefault());
            }

            return _unknownLocation;
        });
    }

    public static bool Initialized { get; private set; }

    private static readonly string _DB_PATH = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\IPDatabase\GeoLite2-City.mmdb");
    public static bool CheckDB()
    {
        return File.Exists(_DB_PATH);
    }

    private static DatabaseReader? _databaseReader;
    private static bool initPending;

    public static async Task Init()
    {
        if (initPending)
        {
            return;
        }

        await Task.Run(() =>
        {

            initPending = true;
            _databaseReader ??= new DatabaseReader(_DB_PATH);
            initPending = false;

            Initialized = true;
        }).ConfigureAwait(true);

        try
        {
            geoWatcher = new GeoCoordinateWatcher();
            geoWatcher.PositionChanged += GeoWatcher_PositionChanged;
            geoWatcher.Start();
        }
        catch (Exception exc)
        {
            LogHelper.Error("Unable to use GeoCoordinateWatcher. Falling back to IP address based method.", exc);
        }

        // If not yet set, fallback to the IP-based location
        CurrentCoordinates ??= await IPToLocationAsync(IPHelper.CurrentIP);
    }

    private static void GeoWatcher_PositionChanged(object? sender, GeoPositionChangedEventArgs<GeoCoordinate> e)
    {
        if (!geoWatcher!.Position.Location.IsUnknown)
        {
            CurrentCoordinates = new Location(geoWatcher.Position.Location.Latitude, geoWatcher.Position.Location.Longitude);
        }
    }
}
