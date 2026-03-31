using JobShadowing.Application.DTOs;
using JobShadowing.Application.DTOs.Projects;

namespace JobShadowing.Application.Interfaces
{
    public interface IProjectService
    {
        Task<ProjectDto> CreateProjectAsync(int userId, CreateProjectDto dto);
        Task<List<ProjectSummaryDto>> GetUserProjectsAsync(int userId);
        Task<ProjectDto> GetProjectByIdAsync(int userId, int projectId);
        Task<PagedResult<TaskSummaryDto>> GetProjectTasksAsync(int userId, int projectId, int page, int pageSize);
        Task<ProjectDto> UpdateProjectAsync(int userId, int projectId, UpdateProjectDto dto);
        Task DeleteProjectAsync(int userId, int projectId);
        Task<bool> CanUserAccessProjectAsync(int userId, int projectId);
    }
}
