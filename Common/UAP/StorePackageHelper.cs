using Microsoft.Win32;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

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
    public static string? GetAppPkgId(uint pid)
    {
        if (Environment.OSVersion.Version <= minVersionForApps)
        {
            //Not Windows 8 or higher, there are no Apps
            return String.Empty;
        }

        IntPtr hProcess = NativeMethods.OpenProcess(NativeMethods.ProcessAccessFlags.QueryLimitedInformation, false, pid);
        if (hProcess == IntPtr.Zero)
        {
            LogHelper.Warning("Unable to retrieve process package id: process cannot be found!");
            return String.Empty;
        }
        try
        {
            //Based on: https://github.com/jimschubert/clr-profiler/blob/master/src/CLRProfiler45Source/WindowsStoreAppHelper/WindowsStoreAppHelper.cs
            uint packageFamilyNameLength = 0;
            string packageFamilyName;
            unsafe
            {
                uint retGetPFName = NativeMethods.GetPackageFamilyName(hProcess, ref packageFamilyNameLength, null);
                if ((retGetPFName == NativeMethods.APPMODEL_ERROR_NO_PACKAGE) || (packageFamilyNameLength == 0))
                {
                    // Not a WindowsStoreApp process
                    return String.Empty;
                }

                // Call again, now that we know the size
                char* packageFamilyNameBld = stackalloc char[(int)packageFamilyNameLength];
                retGetPFName = NativeMethods.GetPackageFamilyName(hProcess, ref packageFamilyNameLength, packageFamilyNameBld);
                if (retGetPFName != NativeMethods.ERROR_SUCCESS)
                {
                    LogHelper.Warning("Unable to retrieve process package id: failed to retrieve family package name!");
                    return String.Empty;
                }

                packageFamilyName = new String(packageFamilyNameBld);
            }

            IntPtr pSID;
            uint ret = NativeMethods.DeriveAppContainerSidFromAppContainerName(packageFamilyName, out pSID);
            if (ret != NativeMethods.S_OK)
            {
                LogHelper.Warning("Unable to retrieve process package id: failed to retrieve package SID!");
                return String.Empty;
            }

            try
            {
                if (NativeMethods.ConvertSidToStringSidW(pSID, out var SID))
                {
                    return SID;
                }

                LogHelper.Warning("Unable to retrieve process package id: SID cannot be converted!");
                return String.Empty;
            }
            finally
            {
                NativeMethods.FreeSid(pSID);
            }
        }
        finally
        {
            NativeMethods.CloseHandle(hProcess);
        }
    }
}
