namespace settAPI.Classes
{
    public class ActivityPeriod
    {
        public int id { get; set; }
        public int session_id { get; set; }
        public WorkSession WorkSession { get; set; } = null!;
        public DateTime period_start { get; set; }
        public DateTime? period_end { get; set; }
        public string status { get; set; } = null!;
    }
}
