using AutoMapper;
using FluentAssertions;
using Xunit;
using JobShadowing.Application.DTOs.Teams;
using JobShadowing.Application.Services;
using JobShadowing.Domain.Enums;
using JobShadowing.Domain.Exceptions;
using JobShadowing.Infrastructure.Data;
using JobShadowing.Infrastructure.Mappings;
using JobShadowing.Tests.Helpers;

namespace JobShadowing.Tests.Unit.Services
{
    public class TeamServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly TeamService _teamService;

        public TeamServiceTests()
        {
            _context = TestDbContextFactory.Create();

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            });
            _mapper = mapperConfig.CreateMapper();

            _teamService = new TeamService(_context, _mapper);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        #region CreateTeam Tests

        [Fact]
        public async Task CreateTeamAsync_ValidData_ReturnsTeamDto()
        {
            // Arrange
            var user = TestDataBuilder.CreateUser(id: 1);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var dto = new CreateTeamDto
            {
                Name = "My Team",
                Description = "Team Description"
            };

            // Act
            var result = await _teamService.CreateTeamAsync(1, dto);

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be("My Team");
            result.Description.Should().Be("Team Description");
            result.MemberCount.Should().Be(1); // Owner is auto-added as member
        }

        [Fact]
        public async Task CreateTeamAsync_OwnerIsAddedAsAdminMember()
        {
            // Arrange
            var user = TestDataBuilder.CreateUser(id: 1);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var dto = new CreateTeamDto { Name = "My Team" };

            // Act
            await _teamService.CreateTeamAsync(1, dto);

            // Assert
            var membership = await _context.TeamMembers.FindAsync(1, 1);
            membership.Should().NotBeNull();
            membership!.Role.Should().Be(TeamRole.Admin);
        }

        #endregion

        #region GetUserTeams Tests

        [Fact]
        public async Task GetUserTeamsAsync_ReturnsOwnedTeams()
        {
            // Arrange
            var user = TestDataBuilder.CreateUser(id: 1);
            var team = TestDataBuilder.CreateTeam(id: 1, name: "My Team", ownerId: 1);

            _context.Users.Add(user);
            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            // Act
            var result = await _teamService.GetUserTeamsAsync(1);

            // Assert
            result.Should().HaveCount(1);
            result[0].Name.Should().Be("My Team");
        }

        [Fact]
        public async Task GetUserTeamsAsync_ReturnsMemberTeams()
        {
            // Arrange
            var owner = TestDataBuilder.CreateUser(id: 1, email: "owner@test.com");
            var member = TestDataBuilder.CreateUser(id: 2, email: "member@test.com");
            var team = TestDataBuilder.CreateTeam(id: 1, name: "Team", ownerId: 1);
            var membership = TestDataBuilder.CreateTeamMember(userId: 2, teamId: 1);

            _context.Users.AddRange(owner, member);
            _context.Teams.Add(team);
            _context.TeamMembers.Add(membership);
            await _context.SaveChangesAsync();

            // Act
            var result = await _teamService.GetUserTeamsAsync(2);

            // Assert
            result.Should().HaveCount(1);
            result[0].Name.Should().Be("Team");
        }

        [Fact]
        public async Task GetUserTeamsAsync_ExcludesNonMemberTeams()
        {
            // Arrange
            var owner = TestDataBuilder.CreateUser(id: 1, email: "owner@test.com");
            var nonMember = TestDataBuilder.CreateUser(id: 2, email: "nonmember@test.com");
            var team = TestDataBuilder.CreateTeam(id: 1, ownerId: 1);

            _context.Users.AddRange(owner, nonMember);
            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            // Act
            var result = await _teamService.GetUserTeamsAsync(2);

            // Assert
            result.Should().BeEmpty();
        }

        #endregion

        #region GetTeamById Tests

        [Fact]
        public async Task GetTeamByIdAsync_TeamOwner_ReturnsTeam()
        {
            // Arrange
            var user = TestDataBuilder.CreateUser(id: 1);
            var team = TestDataBuilder.CreateTeam(id: 1, name: "My Team", ownerId: 1);

            _context.Users.Add(user);
            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            // Act
            var result = await _teamService.GetTeamByIdAsync(1, 1);

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be("My Team");
        }

        [Fact]
        public async Task GetTeamByIdAsync_TeamMember_ReturnsTeam()
        {
            // Arrange
            var owner = TestDataBuilder.CreateUser(id: 1, email: "owner@test.com");
            var member = TestDataBuilder.CreateUser(id: 2, email: "member@test.com");
            var team = TestDataBuilder.CreateTeam(id: 1, name: "Team", ownerId: 1);
            var membership = TestDataBuilder.CreateTeamMember(userId: 2, teamId: 1);

            _context.Users.AddRange(owner, member);
            _context.Teams.Add(team);
            _context.TeamMembers.Add(membership);
            await _context.SaveChangesAsync();

            // Act
            var result = await _teamService.GetTeamByIdAsync(2, 1);

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be("Team");
        }

        [Fact]
        public async Task GetTeamByIdAsync_NonMember_ThrowsForbiddenException()
        {
            // Arrange
            var owner = TestDataBuilder.CreateUser(id: 1, email: "owner@test.com");
            var nonMember = TestDataBuilder.CreateUser(id: 2, email: "nonmember@test.com");
            var team = TestDataBuilder.CreateTeam(id: 1, ownerId: 1);

            _context.Users.AddRange(owner, nonMember);
            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            // Act
            var act = async () => await _teamService.GetTeamByIdAsync(2, 1);

            // Assert
            await act.Should().ThrowAsync<ForbiddenException>();
        }

        [Fact]
        public async Task GetTeamByIdAsync_NonExistent_ThrowsNotFoundException()
        {
            // Arrange
            var user = TestDataBuilder.CreateUser(id: 1);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Act
            var act = async () => await _teamService.GetTeamByIdAsync(1, 999);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        #endregion

        #region AddMember Tests

        [Fact]
        public async Task AddMemberAsync_AsOwner_Succeeds()
        {
            // Arrange
            var owner = TestDataBuilder.CreateUser(id: 1, email: "owner@test.com");
            var newMember = TestDataBuilder.CreateUser(id: 2, email: "newmember@test.com", fullName: "New Member");
            var team = TestDataBuilder.CreateTeam(id: 1, ownerId: 1);

            _context.Users.AddRange(owner, newMember);
            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            var dto = new AddMemberDto { Email = "newmember@test.com" };

            // Act
            var result = await _teamService.AddMemberAsync(1, 1, dto);

            // Assert
            result.Should().NotBeNull();
            result.Email.Should().Be("newmember@test.com");
            result.FullName.Should().Be("New Member");
            result.Role.Should().Be(TeamRole.Member);
        }

        [Fact]
        public async Task AddMemberAsync_NotOwner_ThrowsForbiddenException()
        {
            // Arrange
            var owner = TestDataBuilder.CreateUser(id: 1, email: "owner@test.com");
            var member = TestDataBuilder.CreateUser(id: 2, email: "member@test.com");
            var newMember = TestDataBuilder.CreateUser(id: 3, email: "new@test.com");
            var team = TestDataBuilder.CreateTeam(id: 1, ownerId: 1);
            var membership = TestDataBuilder.CreateTeamMember(userId: 2, teamId: 1);

            _context.Users.AddRange(owner, member, newMember);
            _context.Teams.Add(team);
            _context.TeamMembers.Add(membership);
            await _context.SaveChangesAsync();

            var dto = new AddMemberDto { Email = "new@test.com" };

            // Act
            var act = async () => await _teamService.AddMemberAsync(2, 1, dto);

            // Assert
            await act.Should().ThrowAsync<ForbiddenException>()
                .WithMessage("*owner*");
        }

        [Fact]
        public async Task AddMemberAsync_UserNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var owner = TestDataBuilder.CreateUser(id: 1);
            var team = TestDataBuilder.CreateTeam(id: 1, ownerId: 1);

            _context.Users.Add(owner);
            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            var dto = new AddMemberDto { Email = "nonexistent@test.com" };

            // Act
            var act = async () => await _teamService.AddMemberAsync(1, 1, dto);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("*email*");
        }

        [Fact]
        public async Task AddMemberAsync_AlreadyMember_ThrowsConflictException()
        {
            // Arrange
            var owner = TestDataBuilder.CreateUser(id: 1, email: "owner@test.com");
            var existingMember = TestDataBuilder.CreateUser(id: 2, email: "existing@test.com");
            var team = TestDataBuilder.CreateTeam(id: 1, ownerId: 1);
            var membership = TestDataBuilder.CreateTeamMember(userId: 2, teamId: 1);

            _context.Users.AddRange(owner, existingMember);
            _context.Teams.Add(team);
            _context.TeamMembers.Add(membership);
            await _context.SaveChangesAsync();

            var dto = new AddMemberDto { Email = "existing@test.com" };

            // Act
            var act = async () => await _teamService.AddMemberAsync(1, 1, dto);

            // Assert
            await act.Should().ThrowAsync<ConflictException>()
                .WithMessage("*already a member*");
        }

        #endregion

        #region RemoveMember Tests

        [Fact]
        public async Task RemoveMemberAsync_AsOwner_Succeeds()
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

            // Act
            await _teamService.RemoveMemberAsync(1, 1, 2);

            // Assert
            var removed = await _context.TeamMembers.FindAsync(2, 1);
            removed.Should().BeNull();
        }

        [Fact]
        public async Task RemoveMemberAsync_NotOwner_ThrowsForbiddenException()
        {
            // Arrange
            var owner = TestDataBuilder.CreateUser(id: 1, email: "owner@test.com");
            var member1 = TestDataBuilder.CreateUser(id: 2, email: "member1@test.com");
            var member2 = TestDataBuilder.CreateUser(id: 3, email: "member2@test.com");
            var team = TestDataBuilder.CreateTeam(id: 1, ownerId: 1);
            var membership1 = TestDataBuilder.CreateTeamMember(userId: 2, teamId: 1);
            var membership2 = TestDataBuilder.CreateTeamMember(userId: 3, teamId: 1);

            _context.Users.AddRange(owner, member1, member2);
            _context.Teams.Add(team);
            _context.TeamMembers.AddRange(membership1, membership2);
            await _context.SaveChangesAsync();

            // Act
            var act = async () => await _teamService.RemoveMemberAsync(2, 1, 3);

            // Assert
            await act.Should().ThrowAsync<ForbiddenException>();
        }

        [Fact]
        public async Task RemoveMemberAsync_CannotRemoveOwner_ThrowsConflictException()
        {
            // Arrange
            var owner = TestDataBuilder.CreateUser(id: 1);
            var team = TestDataBuilder.CreateTeam(id: 1, ownerId: 1);

            _context.Users.Add(owner);
            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            // Act
            var act = async () => await _teamService.RemoveMemberAsync(1, 1, 1);

            // Assert
            await act.Should().ThrowAsync<ConflictException>()
                .WithMessage("*owner*");
        }

        [Fact]
        public async Task RemoveMemberAsync_NonMember_ThrowsNotFoundException()
        {
            // Arrange
            var owner = TestDataBuilder.CreateUser(id: 1, email: "owner@test.com");
            var nonMember = TestDataBuilder.CreateUser(id: 2, email: "nonmember@test.com");
            var team = TestDataBuilder.CreateTeam(id: 1, ownerId: 1);

            _context.Users.AddRange(owner, nonMember);
            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            // Act
            var act = async () => await _teamService.RemoveMemberAsync(1, 1, 2);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        #endregion

        #region GetTeamMembers Tests

        [Fact]
        public async Task GetTeamMembersAsync_AsMember_ReturnsMembers()
        {
            // Arrange
            var owner = TestDataBuilder.CreateUser(id: 1, email: "owner@test.com", fullName: "Owner");
            var member = TestDataBuilder.CreateUser(id: 2, email: "member@test.com", fullName: "Member");
            var team = TestDataBuilder.CreateTeam(id: 1, ownerId: 1);
            var ownerMembership = TestDataBuilder.CreateTeamMember(userId: 1, teamId: 1, role: TeamRole.Admin);
            var membership = TestDataBuilder.CreateTeamMember(userId: 2, teamId: 1);

            _context.Users.AddRange(owner, member);
            _context.Teams.Add(team);
            _context.TeamMembers.AddRange(ownerMembership, membership);
            await _context.SaveChangesAsync();

            // Act
            var result = await _teamService.GetTeamMembersAsync(2, 1);

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain(m => m.Email == "owner@test.com" && m.Role == TeamRole.Admin);
            result.Should().Contain(m => m.Email == "member@test.com" && m.Role == TeamRole.Member);
        }

        [Fact]
        public async Task GetTeamMembersAsync_NonMember_ThrowsForbiddenException()
        {
            // Arrange
            var owner = TestDataBuilder.CreateUser(id: 1, email: "owner@test.com");
            var nonMember = TestDataBuilder.CreateUser(id: 2, email: "nonmember@test.com");
            var team = TestDataBuilder.CreateTeam(id: 1, ownerId: 1);

            _context.Users.AddRange(owner, nonMember);
            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            // Act
            var act = async () => await _teamService.GetTeamMembersAsync(2, 1);

            // Assert
            await act.Should().ThrowAsync<ForbiddenException>();
        }

        #endregion

        #region GetTeamProjects Tests

        [Fact]
        public async Task GetTeamProjectsAsync_AsMember_ReturnsProjects()
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
            var result = await _teamService.GetTeamProjectsAsync(2, 1);

            // Assert
            result.Should().HaveCount(1);
            result[0].Name.Should().Be("Team Project");
            result[0].IsTeamProject.Should().BeTrue();
        }

        [Fact]
        public async Task GetTeamProjectsAsync_NonMember_ThrowsForbiddenException()
        {
            // Arrange
            var owner = TestDataBuilder.CreateUser(id: 1, email: "owner@test.com");
            var nonMember = TestDataBuilder.CreateUser(id: 2, email: "nonmember@test.com");
            var team = TestDataBuilder.CreateTeam(id: 1, ownerId: 1);

            _context.Users.AddRange(owner, nonMember);
            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            // Act
            var act = async () => await _teamService.GetTeamProjectsAsync(2, 1);

            // Assert
            await act.Should().ThrowAsync<ForbiddenException>();
        }

        #endregion

        #region Helper Methods Tests

        [Fact]
        public async Task IsUserTeamMemberAsync_Member_ReturnsTrue()
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

            // Act
            var result = await _teamService.IsUserTeamMemberAsync(2, 1);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task IsUserTeamMemberAsync_NonMember_ReturnsFalse()
        {
            // Arrange
            var owner = TestDataBuilder.CreateUser(id: 1, email: "owner@test.com");
            var nonMember = TestDataBuilder.CreateUser(id: 2, email: "nonmember@test.com");
            var team = TestDataBuilder.CreateTeam(id: 1, ownerId: 1);

            _context.Users.AddRange(owner, nonMember);
            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            // Act
            var result = await _teamService.IsUserTeamMemberAsync(2, 1);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task IsUserTeamOwnerAsync_Owner_ReturnsTrue()
        {
            // Arrange
            var owner = TestDataBuilder.CreateUser(id: 1);
            var team = TestDataBuilder.CreateTeam(id: 1, ownerId: 1);

            _context.Users.Add(owner);
            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            // Act
            var result = await _teamService.IsUserTeamOwnerAsync(1, 1);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task IsUserTeamOwnerAsync_NotOwner_ReturnsFalse()
        {
            // Arrange
            var owner = TestDataBuilder.CreateUser(id: 1, email: "owner@test.com");
            var member = TestDataBuilder.CreateUser(id: 2, email: "member@test.com");
            var team = TestDataBuilder.CreateTeam(id: 1, ownerId: 1);

            _context.Users.AddRange(owner, member);
            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            // Act
            var result = await _teamService.IsUserTeamOwnerAsync(2, 1);

            // Assert
            result.Should().BeFalse();
        }

        #endregion
    }
}
