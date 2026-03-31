using System.ComponentModel.DataAnnotations;
using JobShadowing.Application.Validators;
using JobShadowing.Domain.Enums;

namespace JobShadowing.Application.DTOs
{
    public class CreateTaskDto
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 200 characters")]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }

        [EnumDataType(typeof(UserTaskStatus), ErrorMessage = "Invalid status value")]
        public UserTaskStatus Status { get; set; } = UserTaskStatus.Todo;

        [DataType(DataType.DateTime)]
        [FutureDate(ErrorMessage = "Due date must be in the future")]
        public DateTime? DueDate { get; set; }

        public int? ProjectId { get; set; }
    }
}
