using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wokhan.WindowsFirewallNotifier.Common.Extensions
{
    public static class StreamExtensions
    {
        public static IEnumerable<string> ReadLines(this StreamReader src)
        {
            while (!src.EndOfStream)
            {
                yield return src.ReadLine();
            }
        }
    }
}
