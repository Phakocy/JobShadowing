using AutoMapper;
using JobShadowing.Application.DTOs;
using JobShadowing.Application.DTOs.Projects;
using JobShadowing.Application.Interfaces;
using JobShadowing.Domain.Entities;
using JobShadowing.Domain.Exceptions;
using JobShadowing.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace JobShadowing.Application.Services
{
    public class ProjectService : IProjectService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;

        public ProjectService(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<ProjectDto> CreateProjectAsync(int userId, CreateProjectDto dto)
        {
            if (dto.TeamId.HasValue)
            {
                var isMember = await _context.TeamMembers
                    .AnyAsync(tm => tm.TeamId == dto.TeamId.Value && tm.UserId == userId);

                var isOwner = await _context.Teams
                    .AnyAsync(t => t.Id == dto.TeamId.Value && t.OwnerId == userId);

                if (!isMember && !isOwner)
                {
                    throw new ForbiddenException("You must be a member of the team to create a project in it");
                }
            }

            var project = new Project
            {
                Name = dto.Name,
                Description = dto.Description,
                OwnerId = userId,
                TeamId = dto.TeamId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            return await GetProjectByIdAsync(userId, project.Id);
        }

        public async Task<List<ProjectSummaryDto>> GetUserProjectsAsync(int userId)
        {
            var userTeamIds = await _context.TeamMembers
                .Where(tm => tm.UserId == userId)
                .Select(tm => tm.TeamId)
                .ToListAsync();

            var ownedTeamIds = await _context.Teams
                .Where(t => t.OwnerId == userId)
                .Select(t => t.Id)
                .ToListAsync();

            var allTeamIds = userTeamIds.Union(ownedTeamIds).ToList();

            var projects = await _context.Projects
                .Include(p => p.Tasks)
                .Where(p => p.OwnerId == userId || (p.TeamId.HasValue && allTeamIds.Contains(p.TeamId.Value)))
                .Select(p => new ProjectSummaryDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    TaskCount = p.Tasks.Count,
                    IsTeamProject = p.TeamId.HasValue
                })
                .ToListAsync();

            return projects;
        }

        public async Task<ProjectDto> GetProjectByIdAsync(int userId, int projectId)
        {
            var project = await _context.Projects
                .Include(p => p.Owner)
                .Include(p => p.Team)
                .Include(p => p.Tasks)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null)
            {
                throw new NotFoundException("Project", projectId);
            }

            if (!await CanUserAccessProjectAsync(userId, projectId))
            {
                throw new ForbiddenException("You do not have access to this project");
            }

            return _mapper.Map<ProjectDto>(project);
        }

        public async Task<PagedResult<TaskSummaryDto>> GetProjectTasksAsync(int userId, int projectId, int page, int pageSize)
        {
            if (!await CanUserAccessProjectAsync(userId, projectId))
            {
                throw new ForbiddenException("You do not have access to this project");
            }

            var query = _context.Tasks.Where(t => t.ProjectId == projectId);

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var tasks = await query
                .OrderBy(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new TaskSummaryDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    Status = t.Status,
                    DueDate = t.DueDate,
                    IsOverdue = t.DueDate.HasValue && t.DueDate.Value < DateTime.UtcNow
                })
                .ToListAsync();

            return new PagedResult<TaskSummaryDto>
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                HasPrevious = page > 1,
                HasNext = page < totalPages,
                Data = tasks
            };
        }

        public async Task<ProjectDto> UpdateProjectAsync(int userId, int projectId, UpdateProjectDto dto)
        {
            var project = await _context.Projects.FindAsync(projectId);

            if (project == null)
            {
                throw new NotFoundException("Project", projectId);
            }

            if (project.OwnerId != userId)
            {
                throw new ForbiddenException("Only the project owner can update this project");
            }

            project.Name = dto.Name;
            project.Description = dto.Description;
            project.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return await GetProjectByIdAsync(userId, projectId);
        }

        public async Task DeleteProjectAsync(int userId, int projectId)
        {
            var project = await _context.Projects
                .Include(p => p.Team)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null)
            {
                throw new NotFoundException("Project", projectId);
            }

            var canDelete = project.OwnerId == userId ||
                (project.Team != null && project.Team.OwnerId == userId);

            if (!canDelete)
            {
                throw new ForbiddenException("You do not have permission to delete this project");
            }

            var tasks = await _context.Tasks.Where(t => t.ProjectId == projectId).ToListAsync();
            foreach (var task in tasks)
            {
                task.ProjectId = null;
            }

            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> CanUserAccessProjectAsync(int userId, int projectId)
        {
            var project = await _context.Projects
                .Include(p => p.Team)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null) return false;
            if (project.OwnerId == userId) return true;
            if (!project.TeamId.HasValue) return false;
            if (project.Team?.OwnerId == userId) return true;

            return await _context.TeamMembers
                .AnyAsync(tm => tm.TeamId == project.TeamId && tm.UserId == userId);
        }
    }
}
