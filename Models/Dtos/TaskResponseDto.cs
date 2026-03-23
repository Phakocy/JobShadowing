using JobShadowing.Models.Entities;

namespace JobShadowing.Models.Dtos
{
    public class TaskResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public UserTaskStatus Status { get; set; }
        public string StatusDisplay { get; set; } = string.Empty;
        public DateTime? DueDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsOverdue { get; set; }
    }
}
