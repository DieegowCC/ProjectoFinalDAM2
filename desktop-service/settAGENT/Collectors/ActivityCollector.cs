using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

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

        private static readonly TimeSpan InactivityThreshold = TimeSpan.FromMinutes(5);

        public static bool IsUserActive()
        {
            var info = new LASTINPUTINFO();
            info.cbSize = (uint)Marshal.SizeOf(info);
            GetLastInputInfo(ref info);

            uint idleTimeMs = (uint)Environment.TickCount - info.dwTime;
            return idleTimeMs < InactivityThreshold.TotalMilliseconds;
        }
    }
}
