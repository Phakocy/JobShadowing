namespace JobShadowing.Models.Settings
{
    public class CorsSettings
    {
        // Allowed origins for CORS
        public string[] AllowedOrigins { get; set; } = Array.Empty<string>();

        // Whether to allow credentials
        public bool AllowCredentials { get; set; } = true;
    }
}
