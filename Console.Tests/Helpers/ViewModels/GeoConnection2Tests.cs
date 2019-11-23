using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MaxMind.Db;
using MaxMind.GeoIP2;

namespace Wokhan.WindowsFirewallNotifier.Console.Helpers.ViewModels
{
    [TestClass()]
    public class GeoConnection2Tests
    {
        class TestItem
        {
            internal string IP { get; set; }
            internal string CountryCode { get; set; }
            internal string City { get; set; }
            internal double Latitude { get; set; }
            internal double Longitude { get; set; }
        }

        [TestMethod()]
        public void InitDatabaseTest()
        {
            DatabaseReader reader = GeoConnection2.InitDatabaseReader(null);
            List<TestItem> testItems = new List<TestItem>
            {
                 new TestItem() { IP = "62.2.105.148", CountryCode = "CH", City = "Goldau", Latitude=47.0476, Longitude=8.5462 }, // upc gateway
                 new TestItem() { IP = "52.142.84.61", CountryCode = "IE", City = "Dublin",Latitude=53.3338, Longitude=-6.2488 }, //  microsoft ireland
                 new TestItem() { IP = "52.200.121.83", CountryCode = "US", City = "Ashburn",Latitude=39.0481, Longitude=-77.4728 }, //  origin
                 new TestItem() { IP = "2001:4860:4860::8888", CountryCode = "US", City = null,  Latitude=37.751, Longitude=-97.822 }, //  google dns
            };

            testItems.ForEach(item =>
            {
                var city = reader.City(item.IP);
                Assert.IsNotNull(city);
                Log($"IP={item.IP}, IsoCode={city.Country.IsoCode}, CountryName={city.Country.Name}, MostSpecifiySubivision={city.MostSpecificSubdivision.Name}, MostSpecifiySubivision.IsoCode={city.MostSpecificSubdivision.IsoCode}" +
                    $", City={city.City.Name}, {city.Postal.Code}, Latitude={city.Location.Latitude}, Longitude={city.Location.Longitude}, Population={city.Location.PopulationDensity}");
                Assert.IsTrue(item.CountryCode == city.Country.IsoCode);
                Assert.IsTrue(item.City == city.City.Name);
                Assert.AreEqual(item.Latitude, city.Location.Latitude);
                Assert.AreEqual(item.Longitude, city.Location.Longitude);
            });
        }

        private void Log(string msg)
        {
            System.Console.WriteLine(msg + "\n");
        }
    }
}