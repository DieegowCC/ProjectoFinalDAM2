namespace settAPI.Classes
{
    // SEGUIR AQUÍ, https://claude.ai/share/821499d1-f429-4cc5-b1f0-103654bf7824 (LINK CLAUDE CONVERSACIÓN)
    public class WorkSession
    {
        public int worker_id { get; set; }
        public DateTime started_at { get; set; }
        public DateTime ended_at { get; set; }
        public int total_minutes { get; set; }
    }
}
