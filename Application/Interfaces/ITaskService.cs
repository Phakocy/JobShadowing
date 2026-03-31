using JobShadowing.Application.DTOs;

namespace JobShadowing.Application.Interfaces
{
    public interface ITaskService
    {
        Task<PagedResult<TaskSummaryDto>> GetAllTasksAsync(TaskQueryParameters queryParams);
    }
}
