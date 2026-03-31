namespace JobShadowing.Application.DTOs
{
    /// <summary>
    /// Response DTO for task attachment
    /// </summary>
    public class TaskAttachmentDto
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime UploadedAt { get; set; }
        public int UploadedByUserId { get; set; }
        public string UploadedByName { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
    }
}
