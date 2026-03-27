namespace settAPI.Classes
{
    public class WorkSession
    {
        public int id {  get; set; }
        public int worker_id { get; set; }
        public Worker Worker { get; set; } = null!;
        public DateTime started_at { get; set; }
        public DateTime? ended_at{ get; set; }
        public int? total_minutes { get; set; }
    }
}
