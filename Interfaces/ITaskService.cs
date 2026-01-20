using JobShadowing.Models.Dtos;
using JobShadowing.Models.Entities;
using JobShadowing.Models.QueryParams;

namespace JobShadowing.Interfaces
{
    public interface ITaskService
    {
        Task<PagedResult<TaskSummaryDto>> GetAllTasksAsync(TaskQueryParameters queryParams);
    }
}
