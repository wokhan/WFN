using System.Collections.Generic;

using Wokhan.Core.Core;
using Wokhan.WindowsFirewallNotifier.Common.Logging;

namespace Wokhan.WindowsFirewallNotifier.Common.Processes
{
    public static class ServiceNameResolver
    {
        internal static Dictionary<int, ServiceInfoResult> services = ProcessHelper.GetAllServicesByPidWMI();

        private static void Reload()
        {
            services = ProcessHelper.GetAllServicesByPidWMI();
        }

        public static string GetServicName(int pid)
        {
            if (!services.ContainsKey(pid))
            {
                Reload();
            }

            return services.TryGetValue(pid, out ServiceInfoResult? service) ? service.Name : "-";
        }

        public static ServiceInfoResult? GetServiceInfo(int pid, string fileName)
        {
            if (services.TryGetValue(pid, out ServiceInfoResult? svcInfo))
            {
                LogHelper.Debug($"Service detected for '{fileName}': '{svcInfo.Name}'");
                return svcInfo;
            }
            else
            {
                //ProcessHelper.GetService(pid, threadid, path, protocol, localPort, target, targetPort, out svc, out svcdsc, out unsure);
                LogHelper.Debug($"No service detected for '{fileName}'");
                return null;
            }
        }
    }
}
