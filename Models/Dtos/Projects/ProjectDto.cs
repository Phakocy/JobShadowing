using JobShadowing.Models.Dtos.Auth;
using JobShadowing.Models.Dtos.Teams;

namespace JobShadowing.Models.Dtos.Projects
{
    public class ProjectDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public UserDto Owner { get; set; } = null!;
        public TeamSummaryDto? Team { get; set; }
        public int TaskCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
