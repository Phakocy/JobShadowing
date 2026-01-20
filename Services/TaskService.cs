using JobShadowing.Data;
using JobShadowing.Interfaces;
using JobShadowing.Models.Dtos;
using JobShadowing.Models.Entities;
using JobShadowing.Models.QueryParams;
using Microsoft.EntityFrameworkCore;

namespace JobShadowing.Services
{
    public class TaskService : ITaskService
    {
        private readonly AppDbContext _context;

        public TaskService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<TaskSummaryDto>> GetAllTasksAsync(TaskQueryParameters p)
        {
            IQueryable<TaskItem> query = _context.Tasks;

            if (p.Status.HasValue)
                query = query.Where(t => t.Status == (UserTaskStatus)p.Status.Value);

            if (p.DueBefore.HasValue)
                query = query.Where(t => t.DueDate <= p.DueBefore.Value);

            bool isDescending = p.SortOrder?.ToLower() == "desc";
            query = p.SortBy?.ToLower() switch
            {
                "duedate" => isDescending ? query.OrderByDescending(t => t.DueDate) : query.OrderBy(t => t.DueDate),
                "title" => isDescending ? query.OrderByDescending(t => t.Title) : query.OrderBy(t => t.Title),
                _ => query.OrderBy(t => t.Id)
            };

            var totalItems = await query.CountAsync();

            var items = await query
                .Skip((p.PageNumber - 1) * p.PageSize)
                .Take(p.PageSize)
                .Select(t => new TaskSummaryDto
                {
                    Title = t.Title,
                    Status = t.Status,
                    Description = t.Description,
                    ClosedDate = t.DueDate,
                    StartDate = t.CreatedAt,
                    LastChangeDate = t.UpdatedAt
                })
                .ToListAsync();

            return new PagedResult<TaskSummaryDto>
            {
                TotalCount = totalItems,
                PageNumber = p.PageNumber,
                PageSize = p.PageSize,
                Data = items
            };
        }
    }
}
