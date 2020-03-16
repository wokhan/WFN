using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace Wokhan.WindowsFirewallNotifier.Common.Helpers
{
    public class IconHelper
    {
        private static Dictionary<string, BitmapSource> procIconLst = new Dictionary<string, BitmapSource>();

        private static object procIconLstLocker = new object();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static BitmapSource GetIconFromPath(string path = "", bool defaultIfNotFound = false)
        {
            BitmapSource bitmap;
            // need to lock before trying to get the value else we get duplicates because of concurrency
            lock (procIconLstLocker) {
                if (!procIconLst.TryGetValue(path, out bitmap))
                {
                    Icon ic = null;
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
                            // Using FileHelper.GetFriendlyPath(path) to cover paths like \device\harddiskvolume1\program files etc.
                            string friendlyPath = FileHelper.GetFriendlyPath(path);
                            if (!path.Contains(@"\", StringComparison.Ordinal))
                            {
                                LogHelper.Debug($"Skipped extract icon: '{friendlyPath}' because path has no directory info.");
                                ic = SystemIcons.Application;
                                break;
                            }
                            try
                            {
                                ic = Icon.ExtractAssociatedIcon(friendlyPath) ?? (defaultIfNotFound ? SystemIcons.Application : null);
                            }
                            catch (ArgumentException)
                            {
                                LogHelper.Debug("Unable to extract icon: " + friendlyPath + (!friendlyPath.Equals(path) ? " (" + path + ")" : ""));
                                ic = SystemIcons.Question; //FIXME: Use some generic application icon?
                            }
                            catch (System.IO.FileNotFoundException) //Undocumented exception
                            {
                                LogHelper.Debug("Unable to extract icon: " + friendlyPath + (!friendlyPath.Equals(path) ? " (" + path + ")" : ""));
                                ic = SystemIcons.Warning;
                            }
                            break;
                    }

                    if (ic != null)
                    {
                        //FIXME: Resize the icon to save some memory?
                        bitmap = Imaging.CreateBitmapSourceFromHIcon(ic.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                        bitmap.Freeze();
                        ic.Dispose();
                        procIconLst.TryAdd(path, bitmap);
                    }
                }
            }
            return bitmap;
        }

        public static async Task<BitmapSource> GetIconAsync(string path = "", bool defaultIfNotFound = false)
        {
            return await Task.Run(() => GetIconFromPath(path, defaultIfNotFound)).ConfigureAwait(false);
        }
    }
}