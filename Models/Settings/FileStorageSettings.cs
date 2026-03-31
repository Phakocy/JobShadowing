namespace JobShadowing.Models.Settings
{
    public class FileStorageSettings
    {
        // Base path for storing uploaded files
        public string StoragePath { get; set; } = "uploads";

        // Maximum file size in bytes (default: 10MB)
        public long MaxFileSize { get; set; } = 10 * 1024 * 1024;

        // Allowed file extensions
        public string[] AllowedExtensions { get; set; } = new[]
        {
            ".pdf", ".doc", ".docx", ".txt", ".xls", ".xlsx",
            ".png", ".jpg", ".jpeg", ".gif", ".bmp",
            ".zip", ".rar"
        };
    }
}
