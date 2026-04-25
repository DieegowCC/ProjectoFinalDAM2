namespace settAPI.Classes
{
    public class AppActivity
    {
        public int id {  get; set; }
        public int session_id { get; set; }
        public WorkSession? WorkSession { get; set; }

        public int? applications_id { get; set; }    
        public Application? Application { get; set; }

        public DateTime started_at { get; set; }
        public DateTime? ended_at { get; set; }
        public bool is_foreground { get; set; }     // indica si está en primer plano o no. p.e si tengo el visual en primer plano y el chrome de fondo
                                                    // va a marcar true en visual y false en chrome
    }
}
