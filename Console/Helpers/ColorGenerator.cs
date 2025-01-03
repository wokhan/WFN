using System.Windows.Media;

namespace Wokhan.WindowsFirewallNotifier.Console.Helpers;

public static class ColorGenerator
{
    public static readonly List<Color> Variations = [
        Color.FromRgb(0x00, 0x3f, 0x5c),
        Color.FromRgb(0x2f, 0x4b, 0x7c),
        Color.FromRgb(0x66, 0x51, 0x91),
        Color.FromRgb(0xa0, 0x51, 0x95),
        Color.FromRgb(0xd4, 0x50, 0x87),
        Color.FromRgb(0xf9, 0x5d, 0x6a),
        Color.FromRgb(0xff, 0x7c, 0x43),
        Color.FromRgb(0xff, 0xa6, 0x00)
    ];
}
