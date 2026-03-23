using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using JobShadowing.Data;
using JobShadowing.Models.Dtos;
using JobShadowing.Models.Entities;

namespace JobShadowing.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TasksController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<TasksController> _logger;

        public TasksController(
            AppDbContext context,
            IMapper mapper,
            ILogger<TasksController> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
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
            _logger.LogInformation("Getting tasks - Status: {Status}, IsOverdue: {IsOverdue}, Search: {Search}, Page: {Page}",
                status, isOverdue, search, page);

            var query = _context.Tasks.AsQueryable();

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
        public async Task<ActionResult<TaskResponseDto>> GetTask(int id)
        {
            _logger.LogInformation("Getting task with ID: {TaskId}", id);

            var task = await _context.Tasks.FindAsync(id);

            if (task == null)
            {
                _logger.LogWarning("Task with ID {TaskId} not found", id);
                throw new KeyNotFoundException($"Task with ID {id} not found");
            }

            var taskDto = _mapper.Map<TaskResponseDto>(task);
            return Ok(taskDto);
        }

        [HttpPost]
        [ProducesResponseType(typeof(TaskResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<TaskResponseDto>> CreateTask(CreateTaskDto createTaskDto)
        {
            _logger.LogInformation("Creating new task: {Title}", createTaskDto.Title);

            var taskItem = _mapper.Map<TaskItem>(createTaskDto);

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
        public async Task<ActionResult<TaskResponseDto>> UpdateTask(int id, UpdateTaskDto updateTaskDto)
        {
            _logger.LogInformation("Updating task with ID: {TaskId}", id);

            var existingTask = await _context.Tasks.FindAsync(id);

            if (existingTask == null)
            {
                _logger.LogWarning("Task with ID {TaskId} not found for update", id);
                throw new KeyNotFoundException($"Task with ID {id} not found");
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
        public async Task<ActionResult<TaskResponseDto>> PatchTask(int id, PatchTaskDto patchTaskDto)
        {
            _logger.LogInformation("Patching task with ID: {TaskId}", id);

            var existingTask = await _context.Tasks.FindAsync(id);

            if (existingTask == null)
            {
                _logger.LogWarning("Task with ID {TaskId} not found for patch", id);
                throw new KeyNotFoundException($"Task with ID {id} not found");
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
        public async Task<IActionResult> DeleteTask(int id)
        {
            _logger.LogInformation("Deleting task with ID: {TaskId}", id);

            var task = await _context.Tasks.FindAsync(id);

            if (task == null)
            {
                _logger.LogWarning("Task with ID {TaskId} not found for deletion", id);
                throw new KeyNotFoundException($"Task with ID {id} not found");
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
