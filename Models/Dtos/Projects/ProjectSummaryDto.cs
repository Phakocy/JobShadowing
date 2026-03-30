namespace JobShadowing.Models.Dtos.Projects
{
    public class ProjectSummaryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int TaskCount { get; set; }
        public bool IsTeamProject { get; set; }
    }
}
