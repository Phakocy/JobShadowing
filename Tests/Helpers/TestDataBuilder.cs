using JobShadowing.Domain.Entities;
using JobShadowing.Domain.Enums;

namespace JobShadowing.Tests.Helpers
{
    public class TestDataBuilder
    {
        public static User CreateUser(
            int id = 1,
            string email = "test@example.com",
            string fullName = "Test User",
            string passwordHash = "$2a$11$abcdefghijklmnopqrstuv",
            UserRole role = UserRole.User)
        {
            return new User
            {
                Id = id,
                Email = email,
                FullName = fullName,
                PasswordHash = passwordHash,
                Role = role,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        public static TaskItem CreateTask(
            int id = 1,
            string title = "Test Task",
            string? description = "Test Description",
            UserTaskStatus status = UserTaskStatus.Todo,
            int userId = 1,
            int? projectId = null,
            DateTime? dueDate = null)
        {
            return new TaskItem
            {
                Id = id,
                Title = title,
                Description = description,
                Status = status,
                UserId = userId,
                ProjectId = projectId,
                DueDate = dueDate,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        public static Project CreateProject(
            int id = 1,
            string name = "Test Project",
            string? description = "Test Description",
            int ownerId = 1,
            int? teamId = null)
        {
            return new Project
            {
                Id = id,
                Name = name,
                Description = description,
                OwnerId = ownerId,
                TeamId = teamId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        public static Team CreateTeam(
            int id = 1,
            string name = "Test Team",
            string? description = "Test Description",
            int ownerId = 1)
        {
            return new Team
            {
                Id = id,
                Name = name,
                Description = description,
                OwnerId = ownerId,
                CreatedAt = DateTime.UtcNow
            };
        }

        public static TeamMember CreateTeamMember(
            int userId = 1,
            int teamId = 1,
            TeamRole role = TeamRole.Member)
        {
            return new TeamMember
            {
                UserId = userId,
                TeamId = teamId,
                Role = role,
                JoinedAt = DateTime.UtcNow
            };
        }
    }
}
