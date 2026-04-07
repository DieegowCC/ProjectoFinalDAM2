using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace settAGENT.Collectors
{
    public static class ActiveWindowCollector
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        public static (string title, string processName) GetActiveWindow()
        {
            try
            {
                IntPtr handle = GetForegroundWindow();

                var sb = new StringBuilder(256);
                GetWindowText(handle, sb, 256);
                string title = sb.ToString();

                GetWindowThreadProcessId(handle, out uint pid);
                string processName = Process.GetProcessById((int)pid).ProcessName;

                return (title, processName);
            }
            catch
            {
                return (string.Empty, string.Empty);
            }
        }
    }
}
