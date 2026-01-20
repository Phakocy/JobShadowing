namespace JobShadowing.Models.Entities
{
    public class TaskItem
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        //public required string Title { get; set; }  // C# 8+ feature: required to avoid nullability warnings

        public string? Description { get; set; }

        public UserTaskStatus Status { get; set; } = UserTaskStatus.Todo;

        public DateTime? DueDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }


    public enum UserTaskStatus
    {
        Todo = 0,
        InProgress = 1,
        Done = 2
    }
}
