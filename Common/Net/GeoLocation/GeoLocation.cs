namespace Wokhan.WindowsFirewallNotifier.Common.Net.GeoLocation;

public record GeoLocation(double? Latitude = 0, double? Longitude = 0, string? Continent = null, string? Country = null, string? CountryISOCode = null, string? City = null, int? AccuracyRadius = 0);
