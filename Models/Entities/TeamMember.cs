namespace JobShadowing.Models.Entities
{
    public enum TeamRole
    {
        Member = 0,
        Admin = 1
    }

    public class TeamMember
    {
        public int UserId { get; set; }

        public int TeamId { get; set; }

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        public TeamRole Role { get; set; } = TeamRole.Member;

        // Navigation properties
        public User User { get; set; } = null!;
        public Team Team { get; set; } = null!;
    }
}
