namespace JobShadowing.Models.Dtos
{
    public class TaskItemDto
    {
        public required string Title { get; set; }

        public String? Description { get; set; }

        public DateTime? DueDate { get; set; }
    }
}
