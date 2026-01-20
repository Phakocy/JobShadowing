using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobShadowing.Data;
using JobShadowing.Models.Entities;
using JobShadowing.Models.Dtos;
using JobShadowing.Models.QueryParams;
using JobShadowing.Interfaces;

namespace JobShadowing.Controllers

{
    [Route("api/[controller]")]  
    [ApiController]
    public class TasksController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ITaskService _taskService;

        public TasksController(AppDbContext context, ITaskService taskService)
        {
            _context = context;
            _taskService = taskService;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<TaskSummaryDto>>> GetTasks([FromQuery] TaskQueryParameters queryParams)
        {
            var result = await _taskService.GetAllTasksAsync(queryParams);
            return Ok(result);
        }

        //[HttpGet]
        //public async Task<ActionResult<IEnumerable<TaskItem>>> GetTasks(
        //    [FromQuery] int? status,
        //    [FromQuery] DateTime? dueBefore,
        //    [FromQuery] string? sortBy,
        //    [FromQuery] string sortOrder = "asc",
        //    [FromQuery] int pageNumber = 1,
        //    [FromQuery] int pageSize = 10)
        //{
        //    //return await _context.Tasks.ToListAsync();

        //    IQueryable<TaskItem> query = _context.Tasks;

        //    if (status.HasValue)
        //    {
        //        query = query.Where(t => t.Status == (UserTaskStatus)status.Value);
        //    }

        //    if (dueBefore.HasValue)
        //    {
        //        query = query.Where(t => t.DueDate <= dueBefore.Value);
        //    }

        //    if (!string.IsNullOrWhiteSpace(sortBy))
        //    {
        //        bool isDescending = sortOrder.ToLower() == "desc";

        //        query = sortBy.ToLower() switch
        //        {
        //            "duedate" => isDescending ? query.OrderByDescending(t => t.DueDate) : query.OrderBy(t => t.DueDate),
        //            "title" => isDescending ? query.OrderByDescending(t => t.Title) : query.OrderBy(t => t.Title),
        //            "status" => isDescending ? query.OrderByDescending(t => t.Status) : query.OrderBy(t => t.Status),
        //            _ => query.OrderBy(t => t.Id)
        //        };
        //    }

        //    var itemsToSkip = (pageNumber - 1) * pageSize;
        //    var totalItems = await query.CountAsync();
        //    var items = await query.Skip(itemsToSkip).Take(pageSize).ToListAsync();

        //    return Ok(new
        //    {
        //        TotalCount = totalItems,
        //        PageNumber = pageNumber,
        //        PageSize = pageSize,
        //        Data = items
        //    });

        //    //return await query.ToListAsync();
        //}



        [HttpGet("{id}")]
        public async Task<ActionResult<TaskSummaryDto>> GetTask(int id)
        {
            var task = await _context.Tasks.FindAsync(id);

            if (task == null)
            {
                return NotFound();
            }

            var taskSummaryDto = new TaskSummaryDto
            {
                Title = task.Title,
                Description = task.Description,
                ClosedDate = task.DueDate,
                Status = task.Status,
                LastChangeDate = task.UpdatedAt,
                StartDate = task.CreatedAt
            };

            return taskSummaryDto;
        }



        [HttpPost]
        public async Task<ActionResult<TaskItem>> CreateTask(TaskItemDto taskItemDto)
        {
            var taskItem = new TaskItem
            {
                Title = taskItemDto.Title,
                Description = taskItemDto.Description,
                DueDate = taskItemDto?.DueDate,
            };

            _context.Tasks.Add(taskItem);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTask), new { id = taskItem.Id }, taskItem);
        }



        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTask(int id, TaskItem taskItem)
        {
            if (id != taskItem.Id)
            {
                return BadRequest();
            }

            taskItem.UpdatedAt = DateTime.UtcNow;
            _context.Entry(taskItem).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TaskExists(id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null)
            {
                return NotFound();
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
