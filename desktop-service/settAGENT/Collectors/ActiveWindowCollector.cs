using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace settAGENT.Collectors
{
    // Detecta qué ventana tiene el foco en el escritorio de Windows en este momento.
    //
    // Esto NO existe directamente en .NET — hay que pedírselo a Windows llamando
    // a funciones de la librería user32.dll que viene con el sistema operativo.
    //
    // El mecanismo se llama P/Invoke (Platform Invoke):
    //   1. Declaramos la firma de la función nativa con [DllImport("user32.dll")].
    //   2. La llamamos como si fuera un método estático normal de C#.
    //   3. .NET se encarga de "saltar" al código nativo y traducir los parámetros.
    public static class ActiveWindowCollector
    {
        // Devuelve el "handle" (un IntPtr, una especie de ID interno) de la
        // ventana que está en primer plano.
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        // Copia el TÍTULO de la ventana cuyo handle pasamos al StringBuilder
        // 'text'. CharSet.Unicode → soporta acentos, emojis, etc.
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        // Devuelve el ID del PROCESO al que pertenece la ventana. Lo usamos
        // para luego pedir el nombre del .exe (chrome.exe, devenv.exe, etc.).
        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        // Devuelve (titulo de ventana, nombre del proceso) de la app activa.
        // Si algo falla (la ventana se cierra mientras leemos, no hay foco…)
        // devolvemos strings vacíos para que el Worker lo ignore ese tick.
        public static (string title, string processName) GetActiveWindow()
        {
            try
            {
                IntPtr handle = GetForegroundWindow();

                // Pedimos el título: 256 chars suelen sobrar para cualquier ventana.
                StringBuilder sb = new StringBuilder(256);
                GetWindowText(handle, sb, 256);
                string title = sb.ToString();

                // Pedimos el PID y a partir del PID el nombre del proceso.
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
