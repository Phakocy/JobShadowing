using System.ComponentModel.DataAnnotations;
using JobShadowing.Domain.Enums;

namespace JobShadowing.Application.DTOs
{
    public class UpdateTaskDto
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 200 characters")]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }

        [EnumDataType(typeof(UserTaskStatus), ErrorMessage = "Invalid status value")]
        public UserTaskStatus Status { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? DueDate { get; set; }
    }
}
