using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

using Wokhan.WindowsFirewallNotifier.Common.Logging;

namespace Wokhan.WindowsFirewallNotifier.Common.IO.Files
{
    public static class IconHelper
    {
        private static Dictionary<string, BitmapSource> procIconLst = new Dictionary<string, BitmapSource>();

        private static object procIconLstLocker = new object();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static BitmapSource GetIconFromPath(string? path)
        {
            path = path ?? string.Empty;

            BitmapSource? bitmap;
            // need to lock before trying to get the value else we get duplicates because of concurrency
            lock (procIconLstLocker)
            {
                if (!procIconLst.TryGetValue(path, out bitmap))
                {
                    Icon? ic = null;
                    try
                    {
                        switch (path)
                        {
                            case "System":
                                ic = SystemIcons.WinLogo;
                                break;
                            case "-":
                                ic = SystemIcons.WinLogo;
                                break;
                            case "":
                            case "?error": //FIXME: Use something else?
                            case "Unknown":
                                ic = SystemIcons.Error;
                                break;

                            default:
                                if (!path.Contains(@"\", StringComparison.Ordinal))
                                {
                                    LogHelper.Debug($"Skipped extract icon: '{path}' because path has no directory info.");
                                    ic = SystemIcons.Application;
                                    break;
                                }
                                try
                                {
                                    ic = Icon.ExtractAssociatedIcon(path);
                                }
                                catch (ArgumentException)
                                {
                                    LogHelper.Debug("Unable to extract icon: " + path);
                                    ic = SystemIcons.Application;
                                }
                                catch (System.IO.FileNotFoundException) //Undocumented exception
                                {
                                    LogHelper.Debug("Unable to extract icon: " + path);
                                    ic = SystemIcons.Warning;
                                }
                                break;
                        }

                        ic = ic ?? SystemIcons.Application;

                        //FIXME: Resize the icon to save some memory?
                        bitmap = Imaging.CreateBitmapSourceFromHIcon(ic.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                        bitmap.Freeze();
                        procIconLst.Add(path, bitmap);
                    }
                    finally
                    {
                        ic?.Dispose();
                    }
                }
            }
            return bitmap;
        }

        public static async Task<BitmapSource> GetIconAsync(string? path = "")
        {
            return await Task.Run(() => GetIconFromPath(path)).ConfigureAwait(false);
        }
    }
}