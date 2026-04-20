using System.Runtime.InteropServices;

namespace settAGENT.Collectors
{
    public static class ActivityCollector
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        [DllImport("user32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        public static bool IsUserActive(int thresholdMinutes)
        {
            LASTINPUTINFO info = new LASTINPUTINFO();
            info.cbSize = (uint)Marshal.SizeOf(info);
            GetLastInputInfo(ref info);

            uint idleTimeMs = (uint)Environment.TickCount - info.dwTime;
            return idleTimeMs < TimeSpan.FromMinutes(thresholdMinutes).TotalMilliseconds;
        }
    }
}
