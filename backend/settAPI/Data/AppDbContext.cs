using Microsoft.EntityFrameworkCore;
using settAPI.Classes;

namespace settAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

            // Aquí van los DbSets
            public DbSet<Worker> Workers { get; set; }
            public DbSet<WorkSession> WorkSessions { get; set; }
            public DbSet<AppActivity> AppActivities { get; set; }
            public DbSet<Application> Applications { get; set; }
            public DbSet<ActivityPeriod> ActivityPeriods { get; set; }
            public DbSet<Admin> Admins { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Aquí va el mapeo a los nombres de las tablas

            modelBuilder.Entity<ActivityPeriod>().ToTable("activity_periods");
            modelBuilder.Entity<Admin>().ToTable("admins");
            modelBuilder.Entity<AppActivity>().ToTable("app_activity");
            modelBuilder.Entity<Application>().ToTable("applications");
            modelBuilder.Entity<Worker>().ToTable("workers");
            modelBuilder.Entity<WorkSession>().ToTable("work_session");

        }
    }
}