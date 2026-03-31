namespace JobShadowing.Models.Settings
{
    public class RateLimitSettings
    {
        // Maximum number of requests per window
        public int PermitLimit { get; set; } = 100;

        // Time window in seconds
        public int WindowSeconds { get; set; } = 60;

        // Queue limit for pending requests
        public int QueueLimit { get; set; } = 10;
    }
}
