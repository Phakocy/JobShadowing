using System.Security.Claims;
using JobShadowing.Interfaces;
using JobShadowing.Models.Dtos;
using JobShadowing.Models.Dtos.Projects;
using JobShadowing.Models.Dtos.Teams;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobShadowing.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TeamsController : ControllerBase
    {
        private readonly ITeamService _teamService;
        private readonly ILogger<TeamsController> _logger;

        public TeamsController(ITeamService teamService, ILogger<TeamsController> logger)
        {
            _teamService = teamService;
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
        [ProducesResponseType(typeof(TeamDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<TeamDto>> CreateTeam(CreateTeamDto dto)
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("Creating team for user {UserId}: {Name}", userId, dto.Name);

            var team = await _teamService.CreateTeamAsync(userId, dto);

            return CreatedAtAction(nameof(GetTeam), new { id = team.Id }, team);
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<TeamDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<TeamDto>>> GetTeams()
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("Getting teams for user {UserId}", userId);

            var teams = await _teamService.GetUserTeamsAsync(userId);

            return Ok(teams);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(TeamDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<TeamDto>> GetTeam(int id)
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("Getting team {TeamId} for user {UserId}", id, userId);

            var team = await _teamService.GetTeamByIdAsync(userId, id);

            return Ok(team);
        }

        [HttpGet("{id}/members")]
        [ProducesResponseType(typeof(List<TeamMemberDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<List<TeamMemberDto>>> GetTeamMembers(int id)
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("Getting members for team {TeamId} for user {UserId}", id, userId);

            var members = await _teamService.GetTeamMembersAsync(userId, id);

            return Ok(members);
        }

        [HttpPost("{id}/members")]
        [ProducesResponseType(typeof(TeamMemberDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<TeamMemberDto>> AddMember(int id, AddMemberDto dto)
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("Adding member {Email} to team {TeamId} by user {UserId}", dto.Email, id, userId);

            var member = await _teamService.AddMemberAsync(userId, id, dto);

            return CreatedAtAction(nameof(GetTeamMembers), new { id }, member);
        }

        [HttpDelete("{id}/members/{userId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> RemoveMember(int id, int userId)
        {
            var currentUserId = GetCurrentUserId();
            _logger.LogInformation("Removing user {TargetUserId} from team {TeamId} by user {UserId}", userId, id, currentUserId);

            await _teamService.RemoveMemberAsync(currentUserId, id, userId);

            return NoContent();
        }

        [HttpGet("{id}/projects")]
        [ProducesResponseType(typeof(List<ProjectSummaryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<List<ProjectSummaryDto>>> GetTeamProjects(int id)
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("Getting projects for team {TeamId} for user {UserId}", id, userId);

            var projects = await _teamService.GetTeamProjectsAsync(userId, id);

            return Ok(projects);
        }
    }
}
