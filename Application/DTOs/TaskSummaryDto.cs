using JobShadowing.Domain.Enums;

namespace JobShadowing.Application.DTOs
{
    public class TaskSummaryDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public UserTaskStatus Status { get; set; }
        public DateTime? DueDate { get; set; }
        public bool IsOverdue { get; set; }
    }
}
