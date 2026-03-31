using System.ComponentModel.DataAnnotations;

namespace JobShadowing.Application.DTOs.Teams
{
    public class CreateTeamDto
    {
        [Required(ErrorMessage = "Team name is required")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 100 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }
    }
}
