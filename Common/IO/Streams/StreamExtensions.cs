using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;

namespace Wokhan.WindowsFirewallNotifier.Common.IO.Streams
{
    public static class StreamExtensions
    {
        public static IEnumerable<string> ReadLines(this StreamReader src)
        {
            Contract.Requires(src is object);

            while (!src.EndOfStream)
            {
                yield return src.ReadLine();
            }
        }
    }
}
