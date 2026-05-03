using System.Net.NetworkInformation;

namespace settAGENT.Collectors
{
    // Datos del PC donde corre el agente: MAC, hostname y usuario de Windows.
    // Se llaman una sola vez (al arrancar el agente) — no cambian en runtime.
    public static class SystemInfoCollector
    {
        // Devuelve la MAC de la primera tarjeta de red activa (no loopback).
        // Si no encuentra ninguna, devuelve "UNKNOWN" para que el agente no falle.
        public static string GetMacAddress()
        {
            return NetworkInterface
                .GetAllNetworkInterfaces()
                .Where(nic => nic.OperationalStatus == OperationalStatus.Up
                           && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .Select(nic => nic.GetPhysicalAddress().ToString())
                .FirstOrDefault() ?? "UNKNOWN";
        }

        // El hostname (nombre de red) del PC. Es lo que la API usa para
        // identificar a qué Worker pertenece este agente.
        public static string GetHostname() => Environment.MachineName;

        // El usuario de Windows que está logueado (informativo, solo se usa en logs).
        public static string GetWindowsUsername() => Environment.UserName;
    }
}
