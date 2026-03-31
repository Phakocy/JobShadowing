using FluentAssertions;
using Xunit;
using JobShadowing.API.Controllers;
using JobShadowing.Application.DTOs;
using JobShadowing.Application.DTOs.Projects;
using JobShadowing.Application.Interfaces;
using JobShadowing.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace JobShadowing.Tests.Unit.Controllers
{
    public class ProjectsControllerTests
    {
        private readonly Mock<IProjectService> _projectServiceMock;
        private readonly Mock<ILogger<ProjectsController>> _loggerMock;
        private readonly ProjectsController _controller;

        public ProjectsControllerTests()
        {
            _projectServiceMock = new Mock<IProjectService>();
            _loggerMock = new Mock<ILogger<ProjectsController>>();
            _controller = new ProjectsController(_projectServiceMock.Object, _loggerMock.Object);
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

        #region CreateProject Tests

        [Fact]
        public async Task CreateProject_ValidData_Returns201Created()
        {
            // Arrange
            var dto = new CreateProjectDto { Name = "New Project", Description = "Description" };
            var projectDto = new ProjectDto { Id = 1, Name = "New Project", Description = "Description" };

            _projectServiceMock
                .Setup(s => s.CreateProjectAsync(1, dto))
                .ReturnsAsync(projectDto);

            // Act
            var result = await _controller.CreateProject(dto);

            // Assert
            var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            createdResult.StatusCode.Should().Be(201);
            var project = createdResult.Value.Should().BeOfType<ProjectDto>().Subject;
            project.Name.Should().Be("New Project");
        }

        [Fact]
        public async Task CreateProject_TeamNotMember_ThrowsForbiddenException()
        {
            // Arrange
            var dto = new CreateProjectDto { Name = "Team Project", TeamId = 1 };

            _projectServiceMock
                .Setup(s => s.CreateProjectAsync(1, dto))
                .ThrowsAsync(new ForbiddenException("You must be a member of the team"));

            // Act
            var act = async () => await _controller.CreateProject(dto);

            // Assert
            await act.Should().ThrowAsync<ForbiddenException>();
        }

        #endregion

        #region GetProjects Tests

        [Fact]
        public async Task GetProjects_ReturnsOkWithProjects()
        {
            // Arrange
            var projects = new List<ProjectSummaryDto>
            {
                new() { Id = 1, Name = "Project 1", TaskCount = 5 },
                new() { Id = 2, Name = "Project 2", TaskCount = 3 }
            };

            _projectServiceMock
                .Setup(s => s.GetUserProjectsAsync(1))
                .ReturnsAsync(projects);

            // Act
            var result = await _controller.GetProjects();

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedProjects = okResult.Value.Should().BeAssignableTo<List<ProjectSummaryDto>>().Subject;
            returnedProjects.Should().HaveCount(2);
        }

        #endregion

        #region GetProject Tests

        [Fact]
        public async Task GetProject_Exists_ReturnsOk()
        {
            // Arrange
            var projectDto = new ProjectDto { Id = 1, Name = "My Project" };

            _projectServiceMock
                .Setup(s => s.GetProjectByIdAsync(1, 1))
                .ReturnsAsync(projectDto);

            // Act
            var result = await _controller.GetProject(1);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var project = okResult.Value.Should().BeOfType<ProjectDto>().Subject;
            project.Name.Should().Be("My Project");
        }

        [Fact]
        public async Task GetProject_NotFound_ThrowsNotFoundException()
        {
            // Arrange
            _projectServiceMock
                .Setup(s => s.GetProjectByIdAsync(1, 999))
                .ThrowsAsync(new NotFoundException("Project", 999));

            // Act
            var act = async () => await _controller.GetProject(999);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task GetProject_NoAccess_ThrowsForbiddenException()
        {
            // Arrange
            _projectServiceMock
                .Setup(s => s.GetProjectByIdAsync(1, 1))
                .ThrowsAsync(new ForbiddenException("You do not have access"));

            // Act
            var act = async () => await _controller.GetProject(1);

            // Assert
            await act.Should().ThrowAsync<ForbiddenException>();
        }

        #endregion

        #region UpdateProject Tests

        [Fact]
        public async Task UpdateProject_AsOwner_ReturnsOk()
        {
            // Arrange
            var dto = new UpdateProjectDto { Name = "Updated Name", Description = "Updated Desc" };
            var updatedProject = new ProjectDto { Id = 1, Name = "Updated Name", Description = "Updated Desc" };

            _projectServiceMock
                .Setup(s => s.UpdateProjectAsync(1, 1, dto))
                .ReturnsAsync(updatedProject);

            // Act
            var result = await _controller.UpdateProject(1, dto);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var project = okResult.Value.Should().BeOfType<ProjectDto>().Subject;
            project.Name.Should().Be("Updated Name");
        }

        [Fact]
        public async Task UpdateProject_NotOwner_ThrowsForbiddenException()
        {
            // Arrange
            var dto = new UpdateProjectDto { Name = "Hacked" };

            _projectServiceMock
                .Setup(s => s.UpdateProjectAsync(1, 1, dto))
                .ThrowsAsync(new ForbiddenException("Only the project owner can update"));

            // Act
            var act = async () => await _controller.UpdateProject(1, dto);

            // Assert
            await act.Should().ThrowAsync<ForbiddenException>();
        }

        #endregion

        #region DeleteProject Tests

        [Fact]
        public async Task DeleteProject_AsOwner_Returns204NoContent()
        {
            // Arrange
            _projectServiceMock
                .Setup(s => s.DeleteProjectAsync(1, 1))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.DeleteProject(1);

            // Assert
            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task DeleteProject_NotAuthorized_ThrowsForbiddenException()
        {
            // Arrange
            _projectServiceMock
                .Setup(s => s.DeleteProjectAsync(1, 1))
                .ThrowsAsync(new ForbiddenException("You do not have permission"));

            // Act
            var act = async () => await _controller.DeleteProject(1);

            // Assert
            await act.Should().ThrowAsync<ForbiddenException>();
        }

        #endregion

        #region GetProjectTasks Tests

        [Fact]
        public async Task GetProjectTasks_ReturnsPagedResult()
        {
            // Arrange
            var pagedResult = new PagedResult<TaskSummaryDto>
            {
                Data = new List<TaskSummaryDto>
                {
                    new() { Id = 1, Title = "Task 1" },
                    new() { Id = 2, Title = "Task 2" }
                },
                TotalCount = 2,
                Page = 1,
                PageSize = 10,
                TotalPages = 1,
                HasPrevious = false,
                HasNext = false
            };

            _projectServiceMock
                .Setup(s => s.GetProjectTasksAsync(1, 1, 1, 10))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetProjectTasks(1, 1, 10);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var tasks = okResult.Value.Should().BeOfType<PagedResult<TaskSummaryDto>>().Subject;
            tasks.Data.Should().HaveCount(2);
            tasks.TotalCount.Should().Be(2);
        }

        #endregion
    }
}
