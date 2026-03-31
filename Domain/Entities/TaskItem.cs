using JobShadowing.Domain.Enums;

namespace JobShadowing.Domain.Entities
{
    public class TaskItem
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public UserTaskStatus Status { get; set; } = UserTaskStatus.Todo;

        public DateTime? DueDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Foreign key for User
        public int UserId { get; set; }

        // Foreign key for Project (optional)
        public int? ProjectId { get; set; }

        // Navigation properties
        public User User { get; set; } = null!;
        public Project? Project { get; set; }
        public ICollection<TaskAttachment> Attachments { get; set; } = new List<TaskAttachment>();
    }
}
