using Microsoft.VisualStudio.TestTools.UnitTesting;
using Wokhan.WindowsFirewallNotifier.Console.Helpers.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MaxMind.Db;
using MaxMind.GeoIP2;

namespace Harrwiss.ConsoleProject.Helpers.ViewModels
{
    [TestClass()]
    public class GeoConnection2Tests
    {
        [TestMethod()]
        public void InitDatabaseTest()
        {
            DatabaseReader reader = GeoConnection2.InitDatabaseReader(null);
            List<string> ips = new List<string>
            {
                "62.2.105.148", // upc gateway
                "52.142.84.61",  // svc host
                "52.200.121.83",  // origin
                "2001:4860:4860::8888" // google dns
            };

            ips.ForEach(ip =>
            {
                try
                {
                    var city = reader.City(ip);
                    Log($"IP={ip}, IsoCode={city.Country.IsoCode}, CountryName={city.Country.Name}, MostSpecifiySubivision={city.MostSpecificSubdivision.Name}, MostSpecifiySubivision.IsoCode={city.MostSpecificSubdivision.IsoCode}" +
                        $", City={city.City.Name}, {city.Postal.Code}, Latitude={city.Location.Latitude}, Longitude={city.Location.Longitude}, Population={city.Location.PopulationDensity}");
                } catch (Exception e)
                {
                    Log($"IP={ip} - EXCEPTION: {e.Message}");
                }
            });
        }

        private void Log(string msg)
        {
            System.Console.WriteLine(msg + "\n");
        }
    }
}