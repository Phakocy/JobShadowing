using JobShadowing.Models.Dtos.Projects;
using JobShadowing.Models.Dtos.Teams;

namespace JobShadowing.Interfaces
{
    public interface ITeamService
    {
        Task<TeamDto> CreateTeamAsync(int userId, CreateTeamDto dto);
        Task<List<TeamDto>> GetUserTeamsAsync(int userId);
        Task<TeamDto> GetTeamByIdAsync(int userId, int teamId);
        Task<TeamMemberDto> AddMemberAsync(int userId, int teamId, AddMemberDto dto);
        Task RemoveMemberAsync(int userId, int teamId, int targetUserId);
        Task<List<TeamMemberDto>> GetTeamMembersAsync(int userId, int teamId);
        Task<List<ProjectSummaryDto>> GetTeamProjectsAsync(int userId, int teamId);
        Task<bool> IsUserTeamMemberAsync(int userId, int teamId);
        Task<bool> IsUserTeamOwnerAsync(int userId, int teamId);
    }
}
