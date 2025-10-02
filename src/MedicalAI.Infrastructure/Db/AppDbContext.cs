using Microsoft.EntityFrameworkCore;
using MedicalAI.Core;

namespace MedicalAI.Infrastructure.Db
{
    public class AppDbContext : DbContext
    {
        public DbSet<Patient> Patients => Set<Patient>();
        public DbSet<Study> Studies => Set<Study>();
        public DbSet<Series> Series => Set<Series>();

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

        protected override void OnModelCreating(ModelBuilder b)
        {
            b.Entity<Patient>().HasKey(p => p.Id);
            b.Entity<Study>().HasKey(s => s.Id);
            b.Entity<Series>().HasKey(s => s.Id);
        }
    }
}
