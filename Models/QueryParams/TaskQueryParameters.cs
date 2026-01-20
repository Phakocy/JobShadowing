namespace JobShadowing.Models.QueryParams
{
    public class TaskQueryParameters
    {
        public int? Status { get; set; }
        public DateTime? DueBefore { get; set; }
        public string? SortBy { get; set; }
        public string SortOrder { get; set; } = "asc";
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
