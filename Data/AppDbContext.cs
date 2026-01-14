using JobShadowing.Models;
using Microsoft.EntityFrameworkCore;

namespace JobShadowing.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<TaskItem> Tasks { get; set; }  // Like @Repository in Spring Data JPA

        // Optional: Configure entity behavior
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TaskItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(1000);

                // Create index on Status for faster queries
                entity.HasIndex(e => e.Status);
            });
        }
    }
}
