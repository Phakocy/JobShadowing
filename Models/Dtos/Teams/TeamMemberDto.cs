using JobShadowing.Models.Entities;

namespace JobShadowing.Models.Dtos.Teams
{
    public class TeamMemberDto
    {
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public TeamRole Role { get; set; }
        public DateTime JoinedAt { get; set; }
    }
}
