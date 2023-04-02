using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

using Wokhan.WindowsFirewallNotifier.Common.Logging;

namespace Wokhan.WindowsFirewallNotifier.Common.IO.Files;

public static class IconHelper
{
    private static ConcurrentDictionary<string, BitmapSource> procIconLst = new();

    public static async Task<BitmapSource?> GetIconAsync(string? path = "")
    {
        return await Task.Run(() => procIconLst.GetOrAdd(path ?? String.Empty, RetrieveIcon)).ConfigureAwait(false);
    }

    private static BitmapSource RetrieveIcon(string path)
    {
        BitmapSource? bitmap;
        Icon? ic = null;
        try
        {
            switch (path)
            {
                case "System":
                case "-":
                    ic = SystemIcons.WinLogo;
                    break;

                case "":
                case "?error": //FIXME: Use something else?
                case "Unknown":
                    ic = SystemIcons.Error;
                    break;

                default:
                    if (!path.Contains('\\', StringComparison.Ordinal))
                    {
                        LogHelper.Debug($"Skipped extract icon: '{path}' because path has no directory info.");
                        break;
                    }

                    try
                    {
                        ic = Icon.ExtractAssociatedIcon(path);
                    }
                    catch (ArgumentException)
                    {
                        LogHelper.Debug("Unable to extract icon: " + path);
                    }
                    catch (System.IO.FileNotFoundException) //Undocumented exception
                    {
                        LogHelper.Debug("Unable to extract icon: " + path);
                        ic = SystemIcons.Warning;
                    }
                    break;
            }

            ic ??= SystemIcons.Application;

            //FIXME: Resize the icon to save some memory?
            bitmap = Imaging.CreateBitmapSourceFromHIcon(ic.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            bitmap.Freeze();

            return bitmap;
        }
        finally
        {
            ic?.Dispose();
        }
    }
}