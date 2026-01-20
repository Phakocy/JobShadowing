using JobShadowing.Models.Entities;

namespace JobShadowing.Models.Dtos
{
    public class TaskSummaryDto
    {
        public required string Title { get; set; }

        public UserTaskStatus Status { get; set; }

        public String? Description { get; set; }

        public DateTime? ClosedDate { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime LastChangeDate { get; set; }

    }
}
