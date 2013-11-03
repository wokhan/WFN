using System;
using System.IO;
using System.Windows.Forms;
using System.Reflection;

namespace WindowsFirewallNotifier
{
    public class LogHelper
    {
        public static void Error(string msg, Exception e)
        {
            StreamWriter sw = null;
            try
            {
                sw = new StreamWriter(Path.GetDirectoryName(Application.ExecutablePath) + "\\errors.log", true);
                if (e == null)
                {
                    e = new Exception();
                }
                sw.Write("=== {0:yyyy/MM/dd HH:mm:ss} ===\r\nOS: {1} ({2}bit)\r\n.Net CLR: {3}\r\nPath: {4}\r\nVersion: {5}\r\nMessage: {6}\r\nException: {7}\r\nStacktrace: {8}\r\nUser: {9}\r\n", DateTime.Now, Environment.OSVersion, IntPtr.Size * 8, Environment.Version, Application.ExecutablePath, Application.ProductVersion, msg, e.Message, e.StackTrace, Environment.UserName);
            }
            catch { }
            finally
            {
                if (sw != null)
                {
                    sw.Close();
                }
            }
        }
    }
}
