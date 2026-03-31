using System.ComponentModel.DataAnnotations;

namespace JobShadowing.Application.DTOs.Projects
{
    public class UpdateProjectDto
    {
        [Required(ErrorMessage = "Project name is required")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 100 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }
    }
}
