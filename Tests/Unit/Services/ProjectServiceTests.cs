using AutoMapper;
using FluentAssertions;
using Xunit;
using JobShadowing.Application.DTOs.Projects;
using JobShadowing.Application.Services;
using JobShadowing.Domain.Entities;
using JobShadowing.Domain.Enums;
using JobShadowing.Domain.Exceptions;
using JobShadowing.Infrastructure.Data;
using JobShadowing.Infrastructure.Mappings;
using JobShadowing.Tests.Helpers;

namespace JobShadowing.Tests.Unit.Services
{
    public class ProjectServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ProjectService _projectService;

        public ProjectServiceTests()
        {
            _context = TestDbContextFactory.Create();

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            });
            _mapper = mapperConfig.CreateMapper();

            _projectService = new ProjectService(_context, _mapper);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        #region CreateProject Tests

        [Fact]
        public async Task CreateProjectAsync_ValidPersonalProject_ReturnsProjectDto()
        {
            // Arrange
            var user = TestDataBuilder.CreateUser(id: 1);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var dto = new CreateProjectDto
            {
                Name = "My Project",
                Description = "Project Description"
            };

            // Act
            var result = await _projectService.CreateProjectAsync(1, dto);

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be("My Project");
            result.Description.Should().Be("Project Description");
        }

        [Fact]
        public async Task CreateProjectAsync_TeamProject_AsTeamMember_Succeeds()
        {
            // Arrange
            var owner = TestDataBuilder.CreateUser(id: 1, email: "owner@test.com");
            var member = TestDataBuilder.CreateUser(id: 2, email: "member@test.com");
            var team = TestDataBuilder.CreateTeam(id: 1, ownerId: 1);
            var membership = TestDataBuilder.CreateTeamMember(userId: 2, teamId: 1);

            _context.Users.AddRange(owner, member);
            _context.Teams.Add(team);
            _context.TeamMembers.Add(membership);
            await _context.SaveChangesAsync();

            var dto = new CreateProjectDto
            {
                Name = "Team Project",
                TeamId = 1
            };

            // Act
            var result = await _projectService.CreateProjectAsync(2, dto);

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be("Team Project");
        }

        [Fact]
        public async Task CreateProjectAsync_TeamProject_AsTeamOwner_Succeeds()
        {
            // Arrange
            var owner = TestDataBuilder.CreateUser(id: 1);
            var team = TestDataBuilder.CreateTeam(id: 1, ownerId: 1);

            _context.Users.Add(owner);
            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            var dto = new CreateProjectDto
            {
                Name = "Owner Project",
                TeamId = 1
            };

            // Act
            var result = await _projectService.CreateProjectAsync(1, dto);

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be("Owner Project");
        }

        [Fact]
        public async Task CreateProjectAsync_TeamProject_NotMember_ThrowsForbiddenException()
        {
            // Arrange
            var owner = TestDataBuilder.CreateUser(id: 1, email: "owner@test.com");
            var nonMember = TestDataBuilder.CreateUser(id: 2, email: "nonmember@test.com");
            var team = TestDataBuilder.CreateTeam(id: 1, ownerId: 1);

            _context.Users.AddRange(owner, nonMember);
            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            var dto = new CreateProjectDto
            {
                Name = "Team Project",
                TeamId = 1
            };

            // Act
            var act = async () => await _projectService.CreateProjectAsync(2, dto);

            // Assert
            await act.Should().ThrowAsync<ForbiddenException>()
                .WithMessage("*member of the team*");
        }

        #endregion

        #region GetUserProjects Tests

        [Fact]
        public async Task GetUserProjectsAsync_ReturnsOwnedProjects()
        {
            // Arrange
            var user = TestDataBuilder.CreateUser(id: 1);
            var project = TestDataBuilder.CreateProject(id: 1, name: "My Project", ownerId: 1);

            _context.Users.Add(user);
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            // Act
            var result = await _projectService.GetUserProjectsAsync(1);

            // Assert
            result.Should().HaveCount(1);
            result[0].Name.Should().Be("My Project");
        }

        [Fact]
        public async Task GetUserProjectsAsync_ReturnsTeamProjects()
        {
            // Arrange
            var owner = TestDataBuilder.CreateUser(id: 1, email: "owner@test.com");
            var member = TestDataBuilder.CreateUser(id: 2, email: "member@test.com");
            var team = TestDataBuilder.CreateTeam(id: 1, ownerId: 1);
            var membership = TestDataBuilder.CreateTeamMember(userId: 2, teamId: 1);
            var project = TestDataBuilder.CreateProject(id: 1, name: "Team Project", ownerId: 1, teamId: 1);

            _context.Users.AddRange(owner, member);
            _context.Teams.Add(team);
            _context.TeamMembers.Add(membership);
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            // Act
            var result = await _projectService.GetUserProjectsAsync(2);

            // Assert
            result.Should().HaveCount(1);
            result[0].Name.Should().Be("Team Project");
            result[0].IsTeamProject.Should().BeTrue();
        }

        [Fact]
        public async Task GetUserProjectsAsync_DoesNotReturnOthersProjects()
        {
            // Arrange
            var user1 = TestDataBuilder.CreateUser(id: 1, email: "user1@test.com");
            var user2 = TestDataBuilder.CreateUser(id: 2, email: "user2@test.com");
            var project = TestDataBuilder.CreateProject(id: 1, name: "User1 Project", ownerId: 1);

            _context.Users.AddRange(user1, user2);
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            // Act
            var result = await _projectService.GetUserProjectsAsync(2);

            // Assert
            result.Should().BeEmpty();
        }

        #endregion

        #region GetProjectById Tests

        [Fact]
        public async Task GetProjectByIdAsync_OwnerAccess_ReturnsProject()
        {
            // Arrange
            var user = TestDataBuilder.CreateUser(id: 1);
            var project = TestDataBuilder.CreateProject(id: 1, name: "My Project", ownerId: 1);

            _context.Users.Add(user);
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            // Act
            var result = await _projectService.GetProjectByIdAsync(1, 1);

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be("My Project");
        }

        [Fact]
        public async Task GetProjectByIdAsync_NonExistent_ThrowsNotFoundException()
        {
            // Arrange
            var user = TestDataBuilder.CreateUser(id: 1);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Act
            var act = async () => await _projectService.GetProjectByIdAsync(1, 999);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task GetProjectByIdAsync_NoAccess_ThrowsForbiddenException()
        {
            // Arrange
            var user1 = TestDataBuilder.CreateUser(id: 1, email: "user1@test.com");
            var user2 = TestDataBuilder.CreateUser(id: 2, email: "user2@test.com");
            var project = TestDataBuilder.CreateProject(id: 1, ownerId: 1);

            _context.Users.AddRange(user1, user2);
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            // Act
            var act = async () => await _projectService.GetProjectByIdAsync(2, 1);

            // Assert
            await act.Should().ThrowAsync<ForbiddenException>();
        }

        [Fact]
        public async Task GetProjectByIdAsync_TeamMember_CanAccess()
        {
            // Arrange
            var owner = TestDataBuilder.CreateUser(id: 1, email: "owner@test.com");
            var member = TestDataBuilder.CreateUser(id: 2, email: "member@test.com");
            var team = TestDataBuilder.CreateTeam(id: 1, ownerId: 1);
            var membership = TestDataBuilder.CreateTeamMember(userId: 2, teamId: 1);
            var project = TestDataBuilder.CreateProject(id: 1, name: "Team Project", ownerId: 1, teamId: 1);

            _context.Users.AddRange(owner, member);
            _context.Teams.Add(team);
            _context.TeamMembers.Add(membership);
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            // Act
            var result = await _projectService.GetProjectByIdAsync(2, 1);

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be("Team Project");
        }

        #endregion

        #region UpdateProject Tests

        [Fact]
        public async Task UpdateProjectAsync_AsOwner_Succeeds()
        {
            // Arrange
            var user = TestDataBuilder.CreateUser(id: 1);
            var project = TestDataBuilder.CreateProject(id: 1, name: "Old Name", ownerId: 1);

            _context.Users.Add(user);
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            var dto = new UpdateProjectDto
            {
                Name = "New Name",
                Description = "New Description"
            };

            // Act
            var result = await _projectService.UpdateProjectAsync(1, 1, dto);

            // Assert
            result.Name.Should().Be("New Name");
            result.Description.Should().Be("New Description");
        }

        [Fact]
        public async Task UpdateProjectAsync_NotOwner_ThrowsForbiddenException()
        {
            // Arrange
            var user1 = TestDataBuilder.CreateUser(id: 1, email: "user1@test.com");
            var user2 = TestDataBuilder.CreateUser(id: 2, email: "user2@test.com");
            var team = TestDataBuilder.CreateTeam(id: 1, ownerId: 1);
            var membership = TestDataBuilder.CreateTeamMember(userId: 2, teamId: 1);
            var project = TestDataBuilder.CreateProject(id: 1, ownerId: 1, teamId: 1);

            _context.Users.AddRange(user1, user2);
            _context.Teams.Add(team);
            _context.TeamMembers.Add(membership);
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            var dto = new UpdateProjectDto { Name = "Hacked Name" };

            // Act
            var act = async () => await _projectService.UpdateProjectAsync(2, 1, dto);

            // Assert
            await act.Should().ThrowAsync<ForbiddenException>()
                .WithMessage("*owner*");
        }

        [Fact]
        public async Task UpdateProjectAsync_NonExistent_ThrowsNotFoundException()
        {
            // Arrange
            var user = TestDataBuilder.CreateUser(id: 1);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var dto = new UpdateProjectDto { Name = "New Name" };

            // Act
            var act = async () => await _projectService.UpdateProjectAsync(1, 999, dto);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        #endregion

        #region DeleteProject Tests

        [Fact]
        public async Task DeleteProjectAsync_AsOwner_Succeeds()
        {
            // Arrange
            var user = TestDataBuilder.CreateUser(id: 1);
            var project = TestDataBuilder.CreateProject(id: 1, ownerId: 1);

            _context.Users.Add(user);
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            // Act
            await _projectService.DeleteProjectAsync(1, 1);

            // Assert
            var deleted = await _context.Projects.FindAsync(1);
            deleted.Should().BeNull();
        }

        [Fact]
        public async Task DeleteProjectAsync_AsTeamOwner_Succeeds()
        {
            // Arrange
            var owner = TestDataBuilder.CreateUser(id: 1, email: "owner@test.com");
            var projectOwner = TestDataBuilder.CreateUser(id: 2, email: "projowner@test.com");
            var team = TestDataBuilder.CreateTeam(id: 1, ownerId: 1);
            var membership = TestDataBuilder.CreateTeamMember(userId: 2, teamId: 1);
            var project = TestDataBuilder.CreateProject(id: 1, ownerId: 2, teamId: 1);

            _context.Users.AddRange(owner, projectOwner);
            _context.Teams.Add(team);
            _context.TeamMembers.Add(membership);
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            // Act
            await _projectService.DeleteProjectAsync(1, 1);

            // Assert
            var deleted = await _context.Projects.FindAsync(1);
            deleted.Should().BeNull();
        }

        [Fact]
        public async Task DeleteProjectAsync_NotAuthorized_ThrowsForbiddenException()
        {
            // Arrange
            var owner = TestDataBuilder.CreateUser(id: 1, email: "owner@test.com");
            var member = TestDataBuilder.CreateUser(id: 2, email: "member@test.com");
            var team = TestDataBuilder.CreateTeam(id: 1, ownerId: 1);
            var membership = TestDataBuilder.CreateTeamMember(userId: 2, teamId: 1);
            var project = TestDataBuilder.CreateProject(id: 1, ownerId: 1, teamId: 1);

            _context.Users.AddRange(owner, member);
            _context.Teams.Add(team);
            _context.TeamMembers.Add(membership);
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            // Act
            var act = async () => await _projectService.DeleteProjectAsync(2, 1);

            // Assert
            await act.Should().ThrowAsync<ForbiddenException>();
        }

        [Fact]
        public async Task DeleteProjectAsync_SetsTasksProjectIdToNull()
        {
            // Arrange
            var user = TestDataBuilder.CreateUser(id: 1);
            var project = TestDataBuilder.CreateProject(id: 1, ownerId: 1);
            var task = TestDataBuilder.CreateTask(id: 1, userId: 1, projectId: 1);

            _context.Users.Add(user);
            _context.Projects.Add(project);
            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            // Act
            await _projectService.DeleteProjectAsync(1, 1);

            // Assert
            var taskAfter = await _context.Tasks.FindAsync(1);
            taskAfter.Should().NotBeNull();
            taskAfter!.ProjectId.Should().BeNull();
        }

        #endregion

        #region CanUserAccessProject Tests

        [Fact]
        public async Task CanUserAccessProjectAsync_Owner_ReturnsTrue()
        {
            // Arrange
            var user = TestDataBuilder.CreateUser(id: 1);
            var project = TestDataBuilder.CreateProject(id: 1, ownerId: 1);

            _context.Users.Add(user);
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            // Act
            var result = await _projectService.CanUserAccessProjectAsync(1, 1);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task CanUserAccessProjectAsync_TeamMember_ReturnsTrue()
        {
            // Arrange
            var owner = TestDataBuilder.CreateUser(id: 1, email: "owner@test.com");
            var member = TestDataBuilder.CreateUser(id: 2, email: "member@test.com");
            var team = TestDataBuilder.CreateTeam(id: 1, ownerId: 1);
            var membership = TestDataBuilder.CreateTeamMember(userId: 2, teamId: 1);
            var project = TestDataBuilder.CreateProject(id: 1, ownerId: 1, teamId: 1);

            _context.Users.AddRange(owner, member);
            _context.Teams.Add(team);
            _context.TeamMembers.Add(membership);
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            // Act
            var result = await _projectService.CanUserAccessProjectAsync(2, 1);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task CanUserAccessProjectAsync_NonMember_ReturnsFalse()
        {
            // Arrange
            var owner = TestDataBuilder.CreateUser(id: 1, email: "owner@test.com");
            var nonMember = TestDataBuilder.CreateUser(id: 2, email: "nonmember@test.com");
            var team = TestDataBuilder.CreateTeam(id: 1, ownerId: 1);
            var project = TestDataBuilder.CreateProject(id: 1, ownerId: 1, teamId: 1);

            _context.Users.AddRange(owner, nonMember);
            _context.Teams.Add(team);
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            // Act
            var result = await _projectService.CanUserAccessProjectAsync(2, 1);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task CanUserAccessProjectAsync_NonExistentProject_ReturnsFalse()
        {
            // Arrange
            var user = TestDataBuilder.CreateUser(id: 1);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _projectService.CanUserAccessProjectAsync(1, 999);

            // Assert
            result.Should().BeFalse();
        }

        #endregion
    }
}
