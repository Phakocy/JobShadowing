using AutoMapper;
using JobShadowing.Application.DTOs.Auth;
using JobShadowing.Application.DTOs.Projects;
using JobShadowing.Application.DTOs.Teams;
using JobShadowing.Application.Interfaces;
using JobShadowing.Domain.Entities;
using JobShadowing.Domain.Enums;
using JobShadowing.Domain.Exceptions;
using JobShadowing.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace JobShadowing.Application.Services
{
    public class TeamService : ITeamService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;

        public TeamService(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<TeamDto> CreateTeamAsync(int userId, CreateTeamDto dto)
        {
            var team = new Team
            {
                Name = dto.Name,
                Description = dto.Description,
                OwnerId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            var ownerMembership = new TeamMember
            {
                TeamId = team.Id,
                UserId = userId,
                Role = TeamRole.Admin,
                JoinedAt = DateTime.UtcNow
            };

            _context.TeamMembers.Add(ownerMembership);
            await _context.SaveChangesAsync();

            return await GetTeamByIdAsync(userId, team.Id);
        }

        public async Task<List<TeamDto>> GetUserTeamsAsync(int userId)
        {
            var teamIds = await _context.TeamMembers
                .Where(tm => tm.UserId == userId)
                .Select(tm => tm.TeamId)
                .ToListAsync();

            var ownedTeamIds = await _context.Teams
                .Where(t => t.OwnerId == userId)
                .Select(t => t.Id)
                .ToListAsync();

            var allTeamIds = teamIds.Union(ownedTeamIds).Distinct().ToList();

            var teams = await _context.Teams
                .Include(t => t.Owner)
                .Include(t => t.Members)
                .Include(t => t.Projects)
                .Where(t => allTeamIds.Contains(t.Id))
                .ToListAsync();

            return teams.Select(t => new TeamDto
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                Owner = _mapper.Map<UserDto>(t.Owner),
                MemberCount = t.Members.Count,
                ProjectCount = t.Projects.Count,
                CreatedAt = t.CreatedAt
            }).ToList();
        }

        public async Task<TeamDto> GetTeamByIdAsync(int userId, int teamId)
        {
            var team = await _context.Teams
                .Include(t => t.Owner)
                .Include(t => t.Members)
                .Include(t => t.Projects)
                .FirstOrDefaultAsync(t => t.Id == teamId);

            if (team == null)
            {
                throw new NotFoundException("Team", teamId);
            }

            if (!await IsUserTeamMemberAsync(userId, teamId) && !await IsUserTeamOwnerAsync(userId, teamId))
            {
                throw new ForbiddenException("You do not have access to this team");
            }

            return new TeamDto
            {
                Id = team.Id,
                Name = team.Name,
                Description = team.Description,
                Owner = _mapper.Map<UserDto>(team.Owner),
                MemberCount = team.Members.Count,
                ProjectCount = team.Projects.Count,
                CreatedAt = team.CreatedAt
            };
        }

        public async Task<TeamMemberDto> AddMemberAsync(int userId, int teamId, AddMemberDto dto)
        {
            if (!await IsUserTeamOwnerAsync(userId, teamId))
            {
                throw new ForbiddenException("Only the team owner can add members");
            }

            var userToAdd = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());

            if (userToAdd == null)
            {
                throw new NotFoundException($"User with email {dto.Email} not found");
            }

            var existingMember = await _context.TeamMembers
                .FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.UserId == userToAdd.Id);

            if (existingMember != null)
            {
                throw new ConflictException("User is already a member of this team");
            }

            var teamMember = new TeamMember
            {
                TeamId = teamId,
                UserId = userToAdd.Id,
                Role = TeamRole.Member,
                JoinedAt = DateTime.UtcNow
            };

            _context.TeamMembers.Add(teamMember);
            await _context.SaveChangesAsync();

            return new TeamMemberDto
            {
                UserId = userToAdd.Id,
                Email = userToAdd.Email,
                FullName = userToAdd.FullName,
                Role = teamMember.Role,
                JoinedAt = teamMember.JoinedAt
            };
        }

        public async Task RemoveMemberAsync(int userId, int teamId, int targetUserId)
        {
            var team = await _context.Teams.FindAsync(teamId);

            if (team == null)
            {
                throw new NotFoundException("Team", teamId);
            }

            if (!await IsUserTeamOwnerAsync(userId, teamId))
            {
                throw new ForbiddenException("Only the team owner can remove members");
            }

            if (targetUserId == team.OwnerId)
            {
                throw new ConflictException("Cannot remove the team owner from the team");
            }

            var membership = await _context.TeamMembers
                .FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.UserId == targetUserId);

            if (membership == null)
            {
                throw new NotFoundException("User is not a member of this team");
            }

            _context.TeamMembers.Remove(membership);
            await _context.SaveChangesAsync();
        }

        public async Task<List<TeamMemberDto>> GetTeamMembersAsync(int userId, int teamId)
        {
            if (!await IsUserTeamMemberAsync(userId, teamId) && !await IsUserTeamOwnerAsync(userId, teamId))
            {
                throw new ForbiddenException("You do not have access to this team");
            }

            var members = await _context.TeamMembers
                .Include(tm => tm.User)
                .Where(tm => tm.TeamId == teamId)
                .Select(tm => new TeamMemberDto
                {
                    UserId = tm.UserId,
                    Email = tm.User.Email,
                    FullName = tm.User.FullName,
                    Role = tm.Role,
                    JoinedAt = tm.JoinedAt
                })
                .ToListAsync();

            return members;
        }

        public async Task<List<ProjectSummaryDto>> GetTeamProjectsAsync(int userId, int teamId)
        {
            if (!await IsUserTeamMemberAsync(userId, teamId) && !await IsUserTeamOwnerAsync(userId, teamId))
            {
                throw new ForbiddenException("You do not have access to this team");
            }

            var projects = await _context.Projects
                .Include(p => p.Tasks)
                .Where(p => p.TeamId == teamId)
                .Select(p => new ProjectSummaryDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    TaskCount = p.Tasks.Count,
                    IsTeamProject = true
                })
                .ToListAsync();

            return projects;
        }

        public async Task<bool> IsUserTeamMemberAsync(int userId, int teamId)
        {
            return await _context.TeamMembers
                .AnyAsync(tm => tm.TeamId == teamId && tm.UserId == userId);
        }

        public async Task<bool> IsUserTeamOwnerAsync(int userId, int teamId)
        {
            return await _context.Teams
                .AnyAsync(t => t.Id == teamId && t.OwnerId == userId);
        }
    }
}
