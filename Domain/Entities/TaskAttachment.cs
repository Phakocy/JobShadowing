namespace JobShadowing.Domain.Entities
{
    /// <summary>
    /// Represents a file attachment on a task
    /// </summary>
    public class TaskAttachment
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string StoredFileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime UploadedAt { get; set; }
        public int UploadedByUserId { get; set; }

        // Navigation properties
        public TaskItem Task { get; set; } = null!;
        public User UploadedBy { get; set; } = null!;
    }
}
