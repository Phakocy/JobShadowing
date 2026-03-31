using JobShadowing.Application.DTOs.Auth;

namespace JobShadowing.Application.DTOs.Teams
{
    public class TeamDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public UserDto Owner { get; set; } = null!;
        public int MemberCount { get; set; }
        public int ProjectCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
