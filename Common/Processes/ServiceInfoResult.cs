namespace Wokhan.WindowsFirewallNotifier.Common.Helpers
{
    public class ServiceInfoResult
    {
        public ServiceInfoResult(int processId, string name, string displayName, string pathName)
        {
            ProcessId = processId;
            Name = name;
            DisplayName = displayName;
            PathName = pathName;
        }

        public int ProcessId { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string PathName { get; set; }
    }
}

