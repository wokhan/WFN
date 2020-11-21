namespace Wokhan.WindowsFirewallNotifier.Common.Processes
{
    public class ServiceInfoResult
    {
        public ServiceInfoResult(uint processId, string name, string displayName, string pathName)
        {
            ProcessId = processId;
            Name = name;
            DisplayName = displayName;
            PathName = pathName;
        }

        public uint ProcessId { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string PathName { get; set; }
    }
}

