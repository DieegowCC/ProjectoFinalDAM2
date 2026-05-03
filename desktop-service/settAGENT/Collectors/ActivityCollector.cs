using System.Runtime.InteropServices;

namespace settAGENT.Collectors
{
    // Decide si el usuario está "activo" o "inactivo" según cuánto tiempo lleva
    // sin tocar el teclado o el ratón.
    //
    // De nuevo es P/Invoke contra user32.dll: Windows guarda internamente la
    // marca de tiempo (en milisegundos desde que arrancó el equipo) del último
    // evento de input. Comparándola con el reloj del sistema sabemos cuántos
    // ms lleva el usuario "tieso".
    public static class ActivityCollector
    {
        // Estructura que Windows espera/devuelve. LayoutKind.Sequential y los
        // tipos uint son obligatorios para que .NET la marshalle correctamente
        // hacia/desde el código nativo.
        [StructLayout(LayoutKind.Sequential)]
        private struct LASTINPUTINFO
        {
            public uint cbSize;   // tamaño de la estructura (Windows lo exige)
            public uint dwTime;   // tick (ms) del último input
        }

        // Rellena la estructura con la marca de tiempo del último input.
        [DllImport("user32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        // Devuelve true si el usuario tecleó/movió el ratón en los últimos
        // 'thresholdMinutes' minutos. False si lleva más tiempo idle.
        public static bool IsUserActive(int thresholdMinutes)
        {
            LASTINPUTINFO info = new LASTINPUTINFO();
            info.cbSize = (uint)Marshal.SizeOf(info);
            GetLastInputInfo(ref info);

            // Environment.TickCount = ms desde que arrancó Windows.
            // info.dwTime = ms (en la misma escala) del último input.
            // Restar nos da los ms que lleva idle.
            uint idleTimeMs = (uint)Environment.TickCount - info.dwTime;
            return idleTimeMs < TimeSpan.FromMinutes(thresholdMinutes).TotalMilliseconds;
        }
    }
}
