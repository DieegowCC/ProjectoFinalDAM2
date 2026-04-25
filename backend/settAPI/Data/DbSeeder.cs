using settAPI.Classes;

namespace settAPI.Data
{
    public static class DbSeeder
    {
        public static void Seed(AppDbContext db)
        {
            // Si no hay ningún admin, crea el de demo
            if (!db.Admins.Any())
            {
                Admin admin = new Admin
                {
                    username = "admin",
                    password_hash = BCrypt.Net.BCrypt.HashPassword("admin"),
                    created_at = DateTime.UtcNow
                };
                db.Admins.Add(admin);
            }

            // Si no hay ningún worker, crea el worker 1 (el que usa el agente)
            if (!db.Workers.Any())
            {
                Worker worker = new Worker
                {
                    id = 1,
                    name = "Demo Worker",
                    email = "demo@local",
                    hostname = Environment.MachineName,
                    department = null,
                    created_at = DateTime.UtcNow,
                    is_active = true
                };
                db.Workers.Add(worker);
            }

            db.SaveChanges();
        }
    }
}