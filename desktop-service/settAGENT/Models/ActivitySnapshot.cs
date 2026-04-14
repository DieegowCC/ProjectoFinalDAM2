using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace settAGENT.Models
{
    public class AgentSettings
    {
        public string ApiUrl { get; set; } = string.Empty;
        public int CollectionIntervalSeconds { get; set; } = 10;
        public int InactivityThresholdMinutes { get; set; } = 5;
    }

    // Los campos de los nombres son PROVISIONALES
    public class ActivitySnapshot
    {
        // Sistema
        public string MacAddress { get; set; } = string.Empty;
        public string Hostname { get; set; } = string.Empty;
        public string WindowsUsername { get; set; } = string.Empty;

        // App activa
        public string ActiveWindowTitle { get; set; } = string.Empty;
        public string ActiveProcessName { get; set; } = string.Empty;

        // Actividad
        public bool IsUserActive { get; set; }

        // Timestamp
        public DateTime CapturedAt { get; set; } = DateTime.UtcNow;

    }
}
