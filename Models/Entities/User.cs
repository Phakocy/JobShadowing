namespace JobShadowing.Models.Entities
{
    public enum UserRole
    {
        User = 0,
        Admin = 1
    }

    public class User
    {
        public int Id { get; set; }

        public string Email { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public UserRole Role { get; set; } = UserRole.User;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
        public ICollection<Project> OwnedProjects { get; set; } = new List<Project>();
        public ICollection<Team> OwnedTeams { get; set; } = new List<Team>();
        public ICollection<TeamMember> TeamMemberships { get; set; } = new List<TeamMember>();
    }
}
