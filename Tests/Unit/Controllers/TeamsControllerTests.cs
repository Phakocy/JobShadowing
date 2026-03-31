using FluentAssertions;
using Xunit;
using JobShadowing.API.Controllers;
using JobShadowing.Application.DTOs.Auth;
using JobShadowing.Application.DTOs.Projects;
using JobShadowing.Application.DTOs.Teams;
using JobShadowing.Application.Interfaces;
using JobShadowing.Domain.Enums;
using JobShadowing.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace JobShadowing.Tests.Unit.Controllers
{
    public class TeamsControllerTests
    {
        private readonly Mock<ITeamService> _teamServiceMock;
        private readonly Mock<ILogger<TeamsController>> _loggerMock;
        private readonly TeamsController _controller;

        public TeamsControllerTests()
        {
            _teamServiceMock = new Mock<ITeamService>();
            _loggerMock = new Mock<ILogger<TeamsController>>();
            _controller = new TeamsController(_teamServiceMock.Object, _loggerMock.Object);
            SetupUserContext(1);
        }

        private void SetupUserContext(int userId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var user = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        #region CreateTeam Tests

        [Fact]
        public async Task CreateTeam_ValidData_Returns201Created()
        {
            // Arrange
            var dto = new CreateTeamDto { Name = "New Team", Description = "Description" };
            var teamDto = new TeamDto
            {
                Id = 1,
                Name = "New Team",
                Description = "Description",
                Owner = new UserDto { Id = 1, Email = "owner@test.com", FullName = "Owner", Role = UserRole.User },
                MemberCount = 1
            };

            _teamServiceMock
                .Setup(s => s.CreateTeamAsync(1, dto))
                .ReturnsAsync(teamDto);

            // Act
            var result = await _controller.CreateTeam(dto);

            // Assert
            var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            createdResult.StatusCode.Should().Be(201);
            var team = createdResult.Value.Should().BeOfType<TeamDto>().Subject;
            team.Name.Should().Be("New Team");
        }

        #endregion

        #region GetTeams Tests

        [Fact]
        public async Task GetTeams_ReturnsOkWithTeams()
        {
            // Arrange
            var teams = new List<TeamDto>
            {
                new() { Id = 1, Name = "Team 1", MemberCount = 3 },
                new() { Id = 2, Name = "Team 2", MemberCount = 5 }
            };

            _teamServiceMock
                .Setup(s => s.GetUserTeamsAsync(1))
                .ReturnsAsync(teams);

            // Act
            var result = await _controller.GetTeams();

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedTeams = okResult.Value.Should().BeAssignableTo<List<TeamDto>>().Subject;
            returnedTeams.Should().HaveCount(2);
        }

        #endregion

        #region GetTeam Tests

        [Fact]
        public async Task GetTeam_Exists_ReturnsOk()
        {
            // Arrange
            var teamDto = new TeamDto { Id = 1, Name = "My Team", MemberCount = 3 };

            _teamServiceMock
                .Setup(s => s.GetTeamByIdAsync(1, 1))
                .ReturnsAsync(teamDto);

            // Act
            var result = await _controller.GetTeam(1);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var team = okResult.Value.Should().BeOfType<TeamDto>().Subject;
            team.Name.Should().Be("My Team");
        }

        [Fact]
        public async Task GetTeam_NotFound_ThrowsNotFoundException()
        {
            // Arrange
            _teamServiceMock
                .Setup(s => s.GetTeamByIdAsync(1, 999))
                .ThrowsAsync(new NotFoundException("Team", 999));

            // Act
            var act = async () => await _controller.GetTeam(999);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task GetTeam_NotMember_ThrowsForbiddenException()
        {
            // Arrange
            _teamServiceMock
                .Setup(s => s.GetTeamByIdAsync(1, 1))
                .ThrowsAsync(new ForbiddenException("You do not have access"));

            // Act
            var act = async () => await _controller.GetTeam(1);

            // Assert
            await act.Should().ThrowAsync<ForbiddenException>();
        }

        #endregion

        #region GetTeamMembers Tests

        [Fact]
        public async Task GetTeamMembers_AsMember_ReturnsOk()
        {
            // Arrange
            var members = new List<TeamMemberDto>
            {
                new() { UserId = 1, Email = "owner@test.com", FullName = "Owner", Role = TeamRole.Admin },
                new() { UserId = 2, Email = "member@test.com", FullName = "Member", Role = TeamRole.Member }
            };

            _teamServiceMock
                .Setup(s => s.GetTeamMembersAsync(1, 1))
                .ReturnsAsync(members);

            // Act
            var result = await _controller.GetTeamMembers(1);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedMembers = okResult.Value.Should().BeAssignableTo<List<TeamMemberDto>>().Subject;
            returnedMembers.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetTeamMembers_NotMember_ThrowsForbiddenException()
        {
            // Arrange
            _teamServiceMock
                .Setup(s => s.GetTeamMembersAsync(1, 1))
                .ThrowsAsync(new ForbiddenException("You do not have access"));

            // Act
            var act = async () => await _controller.GetTeamMembers(1);

            // Assert
            await act.Should().ThrowAsync<ForbiddenException>();
        }

        #endregion

        #region AddMember Tests

        [Fact]
        public async Task AddMember_AsOwner_Returns201Created()
        {
            // Arrange
            var dto = new AddMemberDto { Email = "newmember@test.com" };
            var memberDto = new TeamMemberDto
            {
                UserId = 2,
                Email = "newmember@test.com",
                FullName = "New Member",
                Role = TeamRole.Member,
                JoinedAt = DateTime.UtcNow
            };

            _teamServiceMock
                .Setup(s => s.AddMemberAsync(1, 1, dto))
                .ReturnsAsync(memberDto);

            // Act
            var result = await _controller.AddMember(1, dto);

            // Assert
            var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            createdResult.StatusCode.Should().Be(201);
            var member = createdResult.Value.Should().BeOfType<TeamMemberDto>().Subject;
            member.Email.Should().Be("newmember@test.com");
        }

        [Fact]
        public async Task AddMember_NotOwner_ThrowsForbiddenException()
        {
            // Arrange
            var dto = new AddMemberDto { Email = "newmember@test.com" };

            _teamServiceMock
                .Setup(s => s.AddMemberAsync(1, 1, dto))
                .ThrowsAsync(new ForbiddenException("Only the team owner can add members"));

            // Act
            var act = async () => await _controller.AddMember(1, dto);

            // Assert
            await act.Should().ThrowAsync<ForbiddenException>();
        }

        [Fact]
        public async Task AddMember_AlreadyMember_ThrowsConflictException()
        {
            // Arrange
            var dto = new AddMemberDto { Email = "existing@test.com" };

            _teamServiceMock
                .Setup(s => s.AddMemberAsync(1, 1, dto))
                .ThrowsAsync(new ConflictException("User is already a member"));

            // Act
            var act = async () => await _controller.AddMember(1, dto);

            // Assert
            await act.Should().ThrowAsync<ConflictException>();
        }

        #endregion

        #region RemoveMember Tests

        [Fact]
        public async Task RemoveMember_AsOwner_Returns204NoContent()
        {
            // Arrange
            _teamServiceMock
                .Setup(s => s.RemoveMemberAsync(1, 1, 2))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.RemoveMember(1, 2);

            // Assert
            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task RemoveMember_NotOwner_ThrowsForbiddenException()
        {
            // Arrange
            _teamServiceMock
                .Setup(s => s.RemoveMemberAsync(1, 1, 2))
                .ThrowsAsync(new ForbiddenException("Only the team owner can remove members"));

            // Act
            var act = async () => await _controller.RemoveMember(1, 2);

            // Assert
            await act.Should().ThrowAsync<ForbiddenException>();
        }

        [Fact]
        public async Task RemoveMember_CannotRemoveOwner_ThrowsConflictException()
        {
            // Arrange
            _teamServiceMock
                .Setup(s => s.RemoveMemberAsync(1, 1, 1))
                .ThrowsAsync(new ConflictException("Cannot remove the team owner"));

            // Act
            var act = async () => await _controller.RemoveMember(1, 1);

            // Assert
            await act.Should().ThrowAsync<ConflictException>();
        }

        #endregion

        #region GetTeamProjects Tests

        [Fact]
        public async Task GetTeamProjects_AsMember_ReturnsOk()
        {
            // Arrange
            var projects = new List<ProjectSummaryDto>
            {
                new() { Id = 1, Name = "Project 1", IsTeamProject = true },
                new() { Id = 2, Name = "Project 2", IsTeamProject = true }
            };

            _teamServiceMock
                .Setup(s => s.GetTeamProjectsAsync(1, 1))
                .ReturnsAsync(projects);

            // Act
            var result = await _controller.GetTeamProjects(1);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedProjects = okResult.Value.Should().BeAssignableTo<List<ProjectSummaryDto>>().Subject;
            returnedProjects.Should().HaveCount(2);
            returnedProjects.Should().OnlyContain(p => p.IsTeamProject);
        }

        [Fact]
        public async Task GetTeamProjects_NotMember_ThrowsForbiddenException()
        {
            // Arrange
            _teamServiceMock
                .Setup(s => s.GetTeamProjectsAsync(1, 1))
                .ThrowsAsync(new ForbiddenException("You do not have access"));

            // Act
            var act = async () => await _controller.GetTeamProjects(1);

            // Assert
            await act.Should().ThrowAsync<ForbiddenException>();
        }

        #endregion
    }
}
