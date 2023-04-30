using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

using Wokhan.WindowsFirewallNotifier.Common.Logging;

namespace Wokhan.WindowsFirewallNotifier.Common.IO.Files;

public static class IconHelper
{
    public static BitmapSource SystemIcon { get; } = CreateImage(SystemIcons.WinLogo);
    public static BitmapSource ErrorIcon { get; } = CreateImage(SystemIcons.Error);
    public static BitmapSource WarningIcon { get; } = CreateImage(SystemIcons.Warning);
    public static BitmapSource ApplicationIcon { get; } = CreateImage(SystemIcons.Application);
    public static BitmapSource UnknownIcon { get; } = CreateImage(SystemIcons.Question);

    private static ConcurrentDictionary<string, BitmapSource> procIconLst = new() { [""] = UnknownIcon, ["System"] = SystemIcon, ["Unknown"] = ErrorIcon };

    public static async Task<BitmapSource?> GetIconAsync(string? path)
    {
        path ??= String.Empty;
        return await Task.Run(() => procIconLst.GetOrAdd(path ?? String.Empty, RetrieveIcon)).ConfigureAwait(false);
    }

    private static BitmapSource RetrieveIcon(string path)
    {
        if (!path.Contains('\\', StringComparison.Ordinal))
        {
            LogHelper.Debug($"Skipped extract icon: '{path}' because path has no directory info.");
        }

        try
        {
            using var icon = Icon.ExtractAssociatedIcon(path);
            if (icon is not null)
            {
                return CreateImage(icon);
            }
        }
        catch (ArgumentException)
        {
            LogHelper.Debug("Unable to extract icon: " + path);
            return ErrorIcon;
        }
        catch (FileNotFoundException) //Undocumented exception
        {
            LogHelper.Debug("Unable to extract icon: " + path);
            return WarningIcon;
        }

        return ApplicationIcon;
    }


    private static BitmapSource CreateImage(Icon source)
    {
        var bitmap = Imaging.CreateBitmapSourceFromHIcon(source.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

        bitmap.Freeze();

        return bitmap;
    }
}