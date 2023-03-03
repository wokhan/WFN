using Microsoft.Win32;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Wokhan.WindowsFirewallNotifier.Common.UAP
{
    public static class StorePackageHelper
    {

        private static XmlNamespaceManager xmlnsm;
        static StorePackageHelper()
        {
            xmlnsm = new XmlNamespaceManager(new NameTable());
            xmlnsm.AddNamespace("x", "http://schemas.microsoft.com/appx/manifest/foundation/windows10");
        }

        public static Task<(string? Name, string? RootFolder, string? Executable, string? Description, string? LogoPath)> GetPackageBasicInfoAsync(string packageName)
        {
            return Task.Run(() =>
            {
                var path = (string?)Registry.ClassesRoot.OpenSubKey($"Local Settings\\Software\\Microsoft\\Windows\\CurrentVersion\\AppModel\\Repository\\Packages\\{packageName}")?.GetValue("PackageRootFolder");
                string? logo = null;
                string? executable = null;
                string? name = null;
                string? description = null;

                if (path is not null)
                {
                    var xdoc = XDocument.Load(Path.Combine(path, "AppxManifest.xml"));

                    logo = xdoc?.XPathSelectElement("x:Package/x:Properties/x:Logo", xmlnsm)?.Value;
                    executable = xdoc?.XPathSelectElement("x:Package/x:Applications/x:Application", xmlnsm)?.Attribute("Executable")?.Value;
                }

                return (name, path, executable, description, logo);
            });
        }
    }
}
