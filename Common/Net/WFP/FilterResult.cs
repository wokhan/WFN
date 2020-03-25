/// <summary>
/// NetshHelper executes netsh commands and parses the resulting xml content.
/// Author: harrwiss / Nov 2019
/// </summary>
namespace Wokhan.WindowsFirewallNotifier.Common.Net.WFP
{
    public class FilterResult
    {
        public int FilterId { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public FiltersContextEnum FoundIn { get; set; } = FiltersContextEnum.NONE;
        public bool HasErrors { get; set; } = false;
    }
}
