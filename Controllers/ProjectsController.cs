using System.Security.Claims;
using JobShadowing.Interfaces;
using JobShadowing.Models.Dtos;
using JobShadowing.Models.Dtos.Projects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobShadowing.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProjectsController : ControllerBase
    {
        private readonly IProjectService _projectService;
        private readonly ILogger<ProjectsController> _logger;

        public ProjectsController(IProjectService projectService, ILogger<ProjectsController> logger)
        {
            _projectService = projectService;
            _logger = logger;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("Invalid user token");
            }
            return userId;
        }

        [HttpPost]
        [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ProjectDto>> CreateProject(CreateProjectDto dto)
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("Creating project for user {UserId}: {Name}", userId, dto.Name);

            var project = await _projectService.CreateProjectAsync(userId, dto);

            return CreatedAtAction(nameof(GetProject), new { id = project.Id }, project);
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<ProjectSummaryDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<ProjectSummaryDto>>> GetProjects()
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("Getting projects for user {UserId}", userId);

            var projects = await _projectService.GetUserProjectsAsync(userId);

            return Ok(projects);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ProjectDto>> GetProject(int id)
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("Getting project {ProjectId} for user {UserId}", id, userId);

            var project = await _projectService.GetProjectByIdAsync(userId, id);

            return Ok(project);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ProjectDto>> UpdateProject(int id, UpdateProjectDto dto)
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("Updating project {ProjectId} for user {UserId}", id, userId);

            var project = await _projectService.UpdateProjectAsync(userId, id, dto);

            return Ok(project);
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteProject(int id)
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("Deleting project {ProjectId} for user {UserId}", id, userId);

            await _projectService.DeleteProjectAsync(userId, id);

            return NoContent();
        }

        [HttpGet("{id}/tasks")]
        [ProducesResponseType(typeof(PagedResult<TaskSummaryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PagedResult<TaskSummaryDto>>> GetProjectTasks(
            int id,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("Getting tasks for project {ProjectId} for user {UserId}", id, userId);

            var tasks = await _projectService.GetProjectTasksAsync(userId, id, page, pageSize);

            return Ok(tasks);
        }
    }
}
