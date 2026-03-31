using FluentAssertions;
using Xunit;
using JobShadowing.API.Controllers;
using JobShadowing.Application.DTOs.Auth;
using JobShadowing.Application.Interfaces;
using JobShadowing.Domain.Enums;
using JobShadowing.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace JobShadowing.Tests.Unit.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<IAuthService> _authServiceMock;
        private readonly Mock<ILogger<AuthController>> _loggerMock;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            _authServiceMock = new Mock<IAuthService>();
            _loggerMock = new Mock<ILogger<AuthController>>();
            _controller = new AuthController(_authServiceMock.Object, _loggerMock.Object);
        }

        #region Register Tests

        [Fact]
        public async Task Register_ValidData_Returns201Created()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Email = "test@test.com",
                Password = "Password123!",
                FullName = "Test User"
            };

            var userDto = new UserDto
            {
                Id = 1,
                Email = "test@test.com",
                FullName = "Test User",
                Role = UserRole.User
            };

            _authServiceMock
                .Setup(s => s.RegisterAsync(It.IsAny<RegisterDto>()))
                .ReturnsAsync(userDto);

            // Act
            var result = await _controller.Register(registerDto);

            // Assert
            var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            createdResult.StatusCode.Should().Be(201);
            createdResult.Value.Should().BeEquivalentTo(userDto);
        }

        [Fact]
        public async Task Register_ServiceCalled_WithCorrectParameters()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Email = "test@test.com",
                Password = "Password123!",
                FullName = "Test User"
            };

            _authServiceMock
                .Setup(s => s.RegisterAsync(It.IsAny<RegisterDto>()))
                .ReturnsAsync(new UserDto { Id = 1, Email = "test@test.com", FullName = "Test User", Role = UserRole.User });

            // Act
            await _controller.Register(registerDto);

            // Assert
            _authServiceMock.Verify(s => s.RegisterAsync(It.Is<RegisterDto>(
                dto => dto.Email == "test@test.com" &&
                       dto.Password == "Password123!" &&
                       dto.FullName == "Test User"
            )), Times.Once);
        }

        [Fact]
        public async Task Register_DuplicateEmail_ThrowsConflictException()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Email = "existing@test.com",
                Password = "Password123!",
                FullName = "Test User"
            };

            _authServiceMock
                .Setup(s => s.RegisterAsync(It.IsAny<RegisterDto>()))
                .ThrowsAsync(new ConflictException("A user with this email already exists"));

            // Act
            var act = async () => await _controller.Register(registerDto);

            // Assert
            await act.Should().ThrowAsync<ConflictException>();
        }

        #endregion

        #region Login Tests

        [Fact]
        public async Task Login_ValidCredentials_Returns200WithToken()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "test@test.com",
                Password = "Password123!"
            };

            var authResponse = new AuthResponseDto
            {
                Token = "jwt.token.here",
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                User = new UserDto
                {
                    Id = 1,
                    Email = "test@test.com",
                    FullName = "Test User",
                    Role = UserRole.User
                }
            };

            _authServiceMock
                .Setup(s => s.LoginAsync(It.IsAny<LoginDto>()))
                .ReturnsAsync(authResponse);

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);

            var response = okResult.Value.Should().BeOfType<AuthResponseDto>().Subject;
            response.Token.Should().Be("jwt.token.here");
            response.User.Email.Should().Be("test@test.com");
        }

        [Fact]
        public async Task Login_InvalidCredentials_ThrowsForbiddenException()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "test@test.com",
                Password = "WrongPassword"
            };

            _authServiceMock
                .Setup(s => s.LoginAsync(It.IsAny<LoginDto>()))
                .ThrowsAsync(new ForbiddenException("Invalid email or password"));

            // Act
            var act = async () => await _controller.Login(loginDto);

            // Assert
            await act.Should().ThrowAsync<ForbiddenException>();
        }

        [Fact]
        public async Task Login_ServiceCalled_WithCorrectParameters()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "test@test.com",
                Password = "Password123!"
            };

            _authServiceMock
                .Setup(s => s.LoginAsync(It.IsAny<LoginDto>()))
                .ReturnsAsync(new AuthResponseDto
                {
                    Token = "token",
                    ExpiresAt = DateTime.UtcNow,
                    User = new UserDto { Id = 1, Email = "test@test.com", FullName = "Test", Role = UserRole.User }
                });

            // Act
            await _controller.Login(loginDto);

            // Assert
            _authServiceMock.Verify(s => s.LoginAsync(It.Is<LoginDto>(
                dto => dto.Email == "test@test.com" && dto.Password == "Password123!"
            )), Times.Once);
        }

        #endregion
    }
}
