using AutoMapper;
using JobShadowing.Data;
using JobShadowing.Interfaces;
using JobShadowing.Models.Dtos.Projects;
using JobShadowing.Models.Dtos.Teams;
using JobShadowing.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace JobShadowing.Services
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

            // Owner is automatically a member with Admin role
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
            // Get teams where user is owner or member
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
                Owner = _mapper.Map<Models.Dtos.Auth.UserDto>(t.Owner),
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
                throw new KeyNotFoundException($"Team with ID {teamId} not found");
            }

            if (!await IsUserTeamMemberAsync(userId, teamId) && !await IsUserTeamOwnerAsync(userId, teamId))
            {
                throw new UnauthorizedAccessException("You do not have access to this team");
            }

            return new TeamDto
            {
                Id = team.Id,
                Name = team.Name,
                Description = team.Description,
                Owner = _mapper.Map<Models.Dtos.Auth.UserDto>(team.Owner),
                MemberCount = team.Members.Count,
                ProjectCount = team.Projects.Count,
                CreatedAt = team.CreatedAt
            };
        }

        public async Task<TeamMemberDto> AddMemberAsync(int userId, int teamId, AddMemberDto dto)
        {
            // Only team owner can add members
            if (!await IsUserTeamOwnerAsync(userId, teamId))
            {
                throw new UnauthorizedAccessException("Only the team owner can add members");
            }

            // Find user by email
            var userToAdd = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());

            if (userToAdd == null)
            {
                throw new KeyNotFoundException($"User with email {dto.Email} not found");
            }

            // Check if already a member
            var existingMember = await _context.TeamMembers
                .FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.UserId == userToAdd.Id);

            if (existingMember != null)
            {
                throw new InvalidOperationException("User is already a member of this team");
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
                throw new KeyNotFoundException($"Team with ID {teamId} not found");
            }

            // Only team owner can remove members
            if (!await IsUserTeamOwnerAsync(userId, teamId))
            {
                throw new UnauthorizedAccessException("Only the team owner can remove members");
            }

            // Cannot remove the team owner
            if (targetUserId == team.OwnerId)
            {
                throw new InvalidOperationException("Cannot remove the team owner from the team");
            }

            var membership = await _context.TeamMembers
                .FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.UserId == targetUserId);

            if (membership == null)
            {
                throw new KeyNotFoundException("User is not a member of this team");
            }

            _context.TeamMembers.Remove(membership);
            await _context.SaveChangesAsync();
        }

        public async Task<List<TeamMemberDto>> GetTeamMembersAsync(int userId, int teamId)
        {
            if (!await IsUserTeamMemberAsync(userId, teamId) && !await IsUserTeamOwnerAsync(userId, teamId))
            {
                throw new UnauthorizedAccessException("You do not have access to this team");
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
                throw new UnauthorizedAccessException("You do not have access to this team");
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
