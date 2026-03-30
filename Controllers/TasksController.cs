using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using JobShadowing.Data;
using JobShadowing.Interfaces;
using JobShadowing.Models.Dtos;
using JobShadowing.Models.Entities;

namespace JobShadowing.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TasksController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<TasksController> _logger;
        private readonly IProjectService _projectService;

        public TasksController(
            AppDbContext context,
            IMapper mapper,
            ILogger<TasksController> logger,
            IProjectService projectService)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _projectService = projectService;
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

        private bool IsAdmin()
        {
            return User.IsInRole(UserRole.Admin.ToString());
        }

        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<TaskSummaryDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResult<TaskSummaryDto>>> GetTasks(
            [FromQuery] UserTaskStatus? status = null,
            [FromQuery] bool? isOverdue = null,
            [FromQuery] string? search = null,
            [FromQuery] string? sortBy = "createdAt",
            [FromQuery] string? sortOrder = "asc",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("Getting tasks for user {UserId} - Status: {Status}, IsOverdue: {IsOverdue}, Search: {Search}, Page: {Page}",
                userId, status, isOverdue, search, page);

            var query = _context.Tasks.AsQueryable();

            // Admin can see all tasks, regular users only see their own
            if (!IsAdmin())
            {
                query = query.Where(t => t.UserId == userId);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(t => t.Title.Contains(search) ||
                    (t.Description != null && t.Description.Contains(search)));
            }

            if (status.HasValue)
            {
                query = query.Where(t => t.Status == status.Value);
            }

            if (isOverdue.HasValue && isOverdue.Value)
            {
                query = query.Where(t => t.DueDate.HasValue && t.DueDate.Value < DateTime.UtcNow);
            }

            query = sortBy?.ToLower() switch
            {
                "title" => sortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(t => t.Title)
                    : query.OrderBy(t => t.Title),
                "duedate" => sortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(t => t.DueDate)
                    : query.OrderBy(t => t.DueDate),
                "status" => sortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(t => t.Status)
                    : query.OrderBy(t => t.Status),
                _ => sortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(t => t.CreatedAt)
                    : query.OrderBy(t => t.CreatedAt)
            };

            var totalCount = await query.CountAsync();

            var tasks = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var taskDtos = _mapper.Map<List<TaskSummaryDto>>(tasks);

            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var result = new PagedResult<TaskSummaryDto>
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                HasPrevious = page > 1,
                HasNext = page < totalPages,
                Data = taskDtos
            };

            return Ok(result);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(TaskResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<TaskResponseDto>> GetTask(int id)
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("Getting task with ID: {TaskId} for user {UserId}", id, userId);

            var task = await _context.Tasks.FindAsync(id);

            if (task == null)
            {
                _logger.LogWarning("Task with ID {TaskId} not found", id);
                throw new KeyNotFoundException($"Task with ID {id} not found");
            }

            // Check ownership (admins can view any task)
            if (!IsAdmin() && task.UserId != userId)
            {
                _logger.LogWarning("User {UserId} attempted to access task {TaskId} owned by {OwnerId}", userId, id, task.UserId);
                throw new UnauthorizedAccessException("You do not have permission to access this task");
            }

            var taskDto = _mapper.Map<TaskResponseDto>(task);
            return Ok(taskDto);
        }

        [HttpPost]
        [ProducesResponseType(typeof(TaskResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<TaskResponseDto>> CreateTask(CreateTaskDto createTaskDto)
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("Creating new task: {Title} for user {UserId}", createTaskDto.Title, userId);

            // If projectId is specified, verify user has access to the project
            if (createTaskDto.ProjectId.HasValue)
            {
                var canAccess = await _projectService.CanUserAccessProjectAsync(userId, createTaskDto.ProjectId.Value);
                if (!canAccess)
                {
                    throw new UnauthorizedAccessException("You do not have access to this project");
                }
            }

            var taskItem = _mapper.Map<TaskItem>(createTaskDto);
            taskItem.UserId = userId;
            taskItem.ProjectId = createTaskDto.ProjectId;

            _context.Tasks.Add(taskItem);
            await _context.SaveChangesAsync();

            var taskDto = _mapper.Map<TaskResponseDto>(taskItem);

            return CreatedAtAction(
                nameof(GetTask),
                new { id = taskItem.Id },
                taskDto);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(TaskResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<TaskResponseDto>> UpdateTask(int id, UpdateTaskDto updateTaskDto)
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("Updating task with ID: {TaskId} for user {UserId}", id, userId);

            var existingTask = await _context.Tasks.FindAsync(id);

            if (existingTask == null)
            {
                _logger.LogWarning("Task with ID {TaskId} not found for update", id);
                throw new KeyNotFoundException($"Task with ID {id} not found");
            }

            // Check ownership (only owner can update)
            if (existingTask.UserId != userId)
            {
                _logger.LogWarning("User {UserId} attempted to update task {TaskId} owned by {OwnerId}", userId, id, existingTask.UserId);
                throw new UnauthorizedAccessException("You do not have permission to update this task");
            }

            _mapper.Map(updateTaskDto, existingTask);
            existingTask.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TaskExists(id))
                {
                    throw new KeyNotFoundException($"Task with ID {id} not found");
                }
                throw;
            }

            var taskDto = _mapper.Map<TaskResponseDto>(existingTask);
            return Ok(taskDto);
        }

        [HttpPatch("{id}")]
        [ProducesResponseType(typeof(TaskResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<TaskResponseDto>> PatchTask(int id, PatchTaskDto patchTaskDto)
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("Patching task with ID: {TaskId} for user {UserId}", id, userId);

            var existingTask = await _context.Tasks.FindAsync(id);

            if (existingTask == null)
            {
                _logger.LogWarning("Task with ID {TaskId} not found for patch", id);
                throw new KeyNotFoundException($"Task with ID {id} not found");
            }

            // Check ownership (only owner can patch)
            if (existingTask.UserId != userId)
            {
                _logger.LogWarning("User {UserId} attempted to patch task {TaskId} owned by {OwnerId}", userId, id, existingTask.UserId);
                throw new UnauthorizedAccessException("You do not have permission to update this task");
            }

            if (patchTaskDto.Title != null)
                existingTask.Title = patchTaskDto.Title;

            if (patchTaskDto.Description != null)
                existingTask.Description = patchTaskDto.Description;

            if (patchTaskDto.Status.HasValue)
                existingTask.Status = patchTaskDto.Status.Value;

            if (patchTaskDto.DueDate.HasValue)
                existingTask.DueDate = patchTaskDto.DueDate.Value;

            existingTask.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TaskExists(id))
                {
                    throw new KeyNotFoundException($"Task with ID {id} not found");
                }
                throw;
            }

            var taskDto = _mapper.Map<TaskResponseDto>(existingTask);
            return Ok(taskDto);
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteTask(int id)
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("Deleting task with ID: {TaskId} for user {UserId}", id, userId);

            var task = await _context.Tasks.FindAsync(id);

            if (task == null)
            {
                _logger.LogWarning("Task with ID {TaskId} not found for deletion", id);
                throw new KeyNotFoundException($"Task with ID {id} not found");
            }

            // Admin can delete any task, regular users only their own
            if (!IsAdmin() && task.UserId != userId)
            {
                _logger.LogWarning("User {UserId} attempted to delete task {TaskId} owned by {OwnerId}", userId, id, task.UserId);
                throw new UnauthorizedAccessException("You do not have permission to delete this task");
            }

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool TaskExists(int id)
        {
            return _context.Tasks.Any(e => e.Id == id);
        }
    }
}
