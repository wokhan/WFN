﻿using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace Wokhan.WindowsFirewallNotifier.Common.Helpers
{
    public class IconHelper
    {
        private static Dictionary<string, BitmapSource> procIconLst = new Dictionary<string, BitmapSource>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static BitmapSource GetIconFromPath(string path, bool defaultIfNotFound = false)
        {
            Icon ic = null;
            switch (path)
            {
                case "System":
                    ic = SystemIcons.WinLogo;
                    break;

                case "?error":
                    ic = SystemIcons.Error;
                    break;

                default:
                    if (File.Exists(path))
                    {
                        ic = Icon.ExtractAssociatedIcon(path) ?? (defaultIfNotFound ? SystemIcons.Application : null);
                    }
                    else
                    {
                        ic = SystemIcons.Warning;
                    }
                    break;
            }

            if (ic != null)
            {
                //FIXME: Resize the icon to save some memory?
                return Imaging.CreateBitmapSourceFromHIcon(ic.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            else
            {
                return null;
            }
        }

        public static async Task<BitmapSource> GetIconAsync(string path, bool defaultIfNotFound = false)
        {
            return await Task<BitmapSource>.Run(() => GetIcon(path, defaultIfNotFound));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static BitmapSource GetIcon(string path, bool defaultIfNotFound = false)
        {
            BitmapSource icon;
            if (!procIconLst.ContainsKey(path))
            {
                icon = GetIconFromPath(path, defaultIfNotFound);
                //FIXME: Resize to save some memory?
                /*BitmapSource iconTMP = GetIcon(path, defaultIfNotFound);
                if (iconTMP == null)
                {
                    procIconLst.Add(path, null);
                    return null;
                }
                icon = new TransformedBitmap(iconTMP, new ScaleTransform(16 / iconTMP.PixelWidth, 16 / iconTMP.PixelHeight));*/
                procIconLst.Add(path, icon);
            }
            else
            {
                icon = procIconLst[path];
            }

            return icon;
        }
    }
}