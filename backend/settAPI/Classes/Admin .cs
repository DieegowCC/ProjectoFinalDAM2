namespace settAPI.Classes
{
    public class Admin
    {
        public int id {  get; set; }
        public string username { get; set; }
        public string password_hash { get; set; }
        public DateTime created_at { get; set; }
    }
}
