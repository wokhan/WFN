using Microsoft.Win32;

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Threading;

using Wokhan.WindowsFirewallNotifier.Common.Logging;

namespace Wokhan.WindowsFirewallNotifier.Common.UAP;

public static partial class StorePackageHelper
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

                executable = xdoc?.XPathSelectElement("x:Package/x:Applications/x:Application", xmlnsm)?.Attribute("Executable")?.Value;

                var logoAsset = xdoc?.XPathSelectElement("x:Package/x:Properties/x:Logo", xmlnsm)?.Value;
                if (logoAsset is not null)
                {
                    var assumedLogo = Path.Combine(path, logoAsset);
                    if (!File.Exists(assumedLogo))
                    {
                        var ext = assumedLogo.Split('.').Last();
                        assumedLogo = $"{assumedLogo[..^ext.Length]}scale-100.{ext}";
                    }
                    if (File.Exists(assumedLogo))
                    {
                        logo = assumedLogo;
                    }
                }
            }

            return (name, path, executable, description, logo);
        });
    }


    static readonly Version minVersionForApps = new Version(6, 2);



    public unsafe static string? GetAppPkgId(uint pid)
    {
        if (Environment.OSVersion.Version <= minVersionForApps)
        {
            //Not Windows 8 or higher, there are no Apps
            return String.Empty;
        }

        var hProcess = NativeMethods.OpenProcess_SafeHandle(PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
        if (hProcess is null)
        {
            LogHelper.Warning("Unable to retrieve process package id: process cannot be found!");
            return String.Empty;
        }
        try
        {
            //Based on: https://github.com/jimschubert/clr-profiler/blob/master/src/CLRProfiler45Source/WindowsStoreAppHelper/WindowsStoreAppHelper.cs
            uint packageFamilyNameLength = 0;
            string packageFamilyName;

            var retGetPFName = NativeMethods.GetPackageFamilyName(hProcess, ref packageFamilyNameLength, null);
            if ((retGetPFName == WIN32_ERROR.APPMODEL_ERROR_NO_PACKAGE) || (packageFamilyNameLength == 0))
            {
                // Not a WindowsStoreApp process
                return String.Empty;
            }

            // Call again, now that we know the size
            char* packageFamilyNameBld = stackalloc char[(int)packageFamilyNameLength];
            retGetPFName = NativeMethods.GetPackageFamilyName(hProcess, ref packageFamilyNameLength, packageFamilyNameBld);
            if (retGetPFName != WIN32_ERROR.ERROR_SUCCESS)
            {
                LogHelper.Warning("Unable to retrieve process package id: failed to retrieve family package name!");
                return String.Empty;
            }

            packageFamilyName = new String(packageFamilyNameBld);

            uint ret = NativeMethods.DeriveAppContainerSidFromAppContainerName(packageFamilyName, out var pSID);
            if (ret != 0)
            {
                LogHelper.Warning("Unable to retrieve process package id: failed to retrieve package SID!");
                return String.Empty;
            }

            try
            {
                if (NativeMethods.ConvertSidToStringSid(pSID, out var SID))
                {
                    return SID.ToString();
                }

                LogHelper.Warning("Unable to retrieve process package id: SID cannot be converted!");
                return String.Empty;
            }
            finally
            {
                //Already freed since it's a safe handle?
                //NativeMethods.FreeSid(pSID);
            }
        }
        finally
        {
            hProcess?.Close();
        }
    }
}
