using System.ComponentModel.DataAnnotations;

namespace JobShadowing.Application.DTOs.Teams
{
    public class AddMemberDto
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;
    }
}
