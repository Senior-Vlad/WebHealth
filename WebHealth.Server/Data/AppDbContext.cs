using Microsoft.EntityFrameworkCore;
using WebHealth.Server.Models;

namespace WebHealth.Server.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<HealthTarget> Targets => Set<HealthTarget>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<HealthTarget>().HasData(
                new HealthTarget {Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Mock Healthy Site", Url = "https://mock/healthy", IsMock = true, IntervalSeconds = 5, IsActive = true},
                new HealthTarget {Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "Mock HTTP 500", Url = "https://mock/500", IsMock = true, IntervalSeconds = 5, IsActive = true},
                new HealthTarget {Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "Mock Down Site", Url = "https://mock/down", IsMock = true, IntervalSeconds = 5, IsActive = true}
                );
        }
    }
}
