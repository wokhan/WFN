namespace Wokhan.WindowsFirewallNotifier.Common.Net.IP;

public record Owner(string? ModuleName, string? ModulePath)
{
    public static readonly Owner System = new Owner("System", "System");
}
