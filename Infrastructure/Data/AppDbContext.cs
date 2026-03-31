using JobShadowing.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JobShadowing.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<TaskItem> Tasks { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<TeamMember> TeamMembers { get; set; }
        public DbSet<TaskAttachment> TaskAttachments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.FullName).IsRequired().HasMaxLength(100);

                entity.HasIndex(e => e.Email).IsUnique();

                entity.HasMany(u => u.Tasks)
                    .WithOne(t => t.User)
                    .HasForeignKey(t => t.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(u => u.OwnedProjects)
                    .WithOne(p => p.Owner)
                    .HasForeignKey(p => p.OwnerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(u => u.OwnedTeams)
                    .WithOne(t => t.Owner)
                    .HasForeignKey(t => t.OwnerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // TaskItem configuration
            modelBuilder.Entity<TaskItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(1000);

                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.ProjectId);

                entity.HasOne(t => t.Project)
                    .WithMany(p => p.Tasks)
                    .HasForeignKey(t => t.ProjectId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Project configuration
            modelBuilder.Entity<Project>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);

                entity.HasIndex(e => e.OwnerId);
                entity.HasIndex(e => e.TeamId);

                entity.HasOne(p => p.Team)
                    .WithMany(t => t.Projects)
                    .HasForeignKey(p => p.TeamId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Team configuration
            modelBuilder.Entity<Team>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);

                entity.HasIndex(e => e.OwnerId);
            });

            // TeamMember configuration (join table)
            modelBuilder.Entity<TeamMember>(entity =>
            {
                entity.HasKey(tm => new { tm.UserId, tm.TeamId });

                entity.HasOne(tm => tm.User)
                    .WithMany(u => u.TeamMemberships)
                    .HasForeignKey(tm => tm.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(tm => tm.Team)
                    .WithMany(t => t.Members)
                    .HasForeignKey(tm => tm.TeamId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // TaskAttachment configuration
            modelBuilder.Entity<TaskAttachment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.StoredFileName).IsRequired().HasMaxLength(500);
                entity.Property(e => e.ContentType).IsRequired().HasMaxLength(100);

                entity.HasIndex(e => e.TaskId);

                entity.HasOne(a => a.Task)
                    .WithMany(t => t.Attachments)
                    .HasForeignKey(a => a.TaskId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(a => a.UploadedBy)
                    .WithMany()
                    .HasForeignKey(a => a.UploadedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
