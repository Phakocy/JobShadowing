using System.ComponentModel.DataAnnotations;
using JobShadowing.Models.Entities;

namespace JobShadowing.Models.Dtos
{
    public class PatchTaskDto
    {
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 200 characters")]
        public string? Title { get; set; }

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }

        [EnumDataType(typeof(UserTaskStatus), ErrorMessage = "Invalid status value")]
        public UserTaskStatus? Status { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? DueDate { get; set; }
    }
}
