using JobShadowing.Domain.Enums;

namespace JobShadowing.Application.DTOs.Teams
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
