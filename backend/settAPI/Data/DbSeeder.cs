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

            // Si no hay ningún worker, crea el worker 1 (el que usa el agente real)
            // y 4 workers de demo más con hostnames ficticios para que aparezcan como Inactivos
            if (!db.Workers.Any())
            {
                Worker realWorker = new Worker
                {
                    name = Environment.UserName,
                    email = $"{Environment.UserName.ToLower()}@sett.local",
                    hostname = Environment.MachineName,
                    department = "Administración",
                    created_at = DateTime.UtcNow,
                    is_active = true
                };
                db.Workers.Add(realWorker);

                Worker[] demoWorkers = new[]
                {
                    new Worker
                    {
                        name = "Ana García",
                        email = "ana.garcia@sett.local",
                        hostname = "DESKTOP-ANA01",
                        department = "Marketing",
                        created_at = DateTime.UtcNow,
                        is_active = true
                    },
                    new Worker
                    {
                        name = "Carlos Ruiz",
                        email = "carlos.ruiz@sett.local",
                        hostname = "DESKTOP-CARLOS",
                        department = "Desarrollo",
                        created_at = DateTime.UtcNow,
                        is_active = true
                    },
                    new Worker
                    {
                        name = "María López",
                        email = "maria.lopez@sett.local",
                        hostname = "DESKTOP-MARIA",
                        department = "Diseño",
                        created_at = DateTime.UtcNow,
                        is_active = true
                    },
                    new Worker
                    {
                        name = "Juan Martín",
                        email = "juan.martin@sett.local",
                        hostname = "DESKTOP-JUAN01",
                        department = "RRHH",
                        created_at = DateTime.UtcNow,
                        is_active = true
                    }
                };
                db.Workers.AddRange(demoWorkers);
            }

            db.SaveChanges();
        }
    }
}
