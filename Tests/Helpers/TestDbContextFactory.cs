using JobShadowing.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace JobShadowing.Tests.Helpers
{
    public static class TestDbContextFactory
    {
        public static AppDbContext Create(string? databaseName = null)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
                .Options;

            var context = new AppDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        public static async Task<AppDbContext> CreateWithDataAsync(string? databaseName = null)
        {
            var context = Create(databaseName);

            var user = TestDataBuilder.CreateUser(id: 1, email: "user@test.com", fullName: "Test User");
            var admin = TestDataBuilder.CreateUser(id: 2, email: "admin@test.com", fullName: "Admin User", role: Domain.Enums.UserRole.Admin);

            context.Users.AddRange(user, admin);
            await context.SaveChangesAsync();

            return context;
        }
    }
}
