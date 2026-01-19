namespace JobShadowing.Models
{
    public class TaskItem
    {
        public int Id { get; set; }  // EF Core recognizes "Id" as primary key by convention

        public string Title { get; set; } = string.Empty;  // default value (prevents null warnings)

        //public required string Title { get; set; }  // C# 8+ feature: required to avoid nullability warnings

        public string? Description { get; set; }  // ? means nullable - new to C# if coming from Java

        public TaskStatus Status { get; set; } = TaskStatus.Todo;

        public DateTime? DueDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }


    public enum TaskStatus
    {
        Todo = 0,
        InProgress = 1,
        Done = 2
    }
}
