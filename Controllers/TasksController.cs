using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobShadowing.Data;
using JobShadowing.Models;

namespace JobShadowing.Controllers

{
    [Route("api/[controller]")]  // Creates route: /api/tasks
    [ApiController]              // Enables automatic model validation, binding, etc.
    public class TasksController : ControllerBase
    {
        private readonly AppDbContext _context;

        // Constructor injection (like @Autowired but cleaner)
        public TasksController(AppDbContext context)
        {
            _context = context;
        }


        [HttpGet]
        public async Task<ActionResult<IEnumerable<TaskItem>>> GetTasks()
        {
            return await _context.Tasks.ToListAsync();
        }


        [HttpGet("{id}")]
        public async Task<ActionResult<TaskItem>> GetTask(int id)
        {
            var task = await _context.Tasks.FindAsync(id);

            if (task == null)
            {
                return NotFound();
            }

            return task;
        }



        [HttpPost]
        public async Task<ActionResult<TaskItem>> CreateTask(TaskItem taskItem)
        {
            _context.Tasks.Add(taskItem);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTask), new { id = taskItem.Id }, taskItem);
        }



        // PUT: api/tasks/5
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


        // DELETE: api/tasks/5
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
