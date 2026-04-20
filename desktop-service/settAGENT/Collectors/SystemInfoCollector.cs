using System.Net.NetworkInformation;

namespace settAGENT.Collectors
{
    public static class SystemInfoCollector
    {
        public static string GetMacAddress()
        {
            return NetworkInterface
                .GetAllNetworkInterfaces()
                .Where(nic => nic.OperationalStatus == OperationalStatus.Up
                           && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .Select(nic => nic.GetPhysicalAddress().ToString())
                .FirstOrDefault() ?? "UNKNOWN";
        }

        public static string GetHostname() => Environment.MachineName;

        public static string GetWindowsUsername() => Environment.UserName;

    }
}
