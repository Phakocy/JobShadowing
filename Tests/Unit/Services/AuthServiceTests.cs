using AutoMapper;
using FluentAssertions;
using Xunit;
using JobShadowing.Application.DTOs.Auth;
using JobShadowing.Application.Services;
using JobShadowing.Domain.Entities;
using JobShadowing.Domain.Enums;
using JobShadowing.Domain.Exceptions;
using JobShadowing.Infrastructure.Mappings;
using JobShadowing.Models.Settings;
using JobShadowing.Tests.Helpers;
using Microsoft.Extensions.Options;
using Moq;

namespace JobShadowing.Tests.Unit.Services
{
    public class AuthServiceTests : IDisposable
    {
        private readonly Infrastructure.Data.AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly IOptions<JwtSettings> _jwtSettings;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            _context = TestDbContextFactory.Create();

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            });
            _mapper = mapperConfig.CreateMapper();

            _jwtSettings = Options.Create(new JwtSettings
            {
                SecretKey = "ThisIsAVeryLongSecretKeyForTestingPurposesOnly123456",
                Issuer = "TestIssuer",
                Audience = "TestAudience",
                ExpirationMinutes = 60
            });

            _authService = new AuthService(_context, _mapper, _jwtSettings);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        #region Registration Tests

        [Fact]
        public async Task RegisterAsync_ValidData_ReturnsUserDto()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Email = "newuser@test.com",
                Password = "Password123!",
                FullName = "New User"
            };

            // Act
            var result = await _authService.RegisterAsync(registerDto);

            // Assert
            result.Should().NotBeNull();
            result.Email.Should().Be("newuser@test.com");
            result.FullName.Should().Be("New User");
            result.Role.Should().Be(UserRole.User);
        }

        [Fact]
        public async Task RegisterAsync_DuplicateEmail_ThrowsConflictException()
        {
            // Arrange
            var existingUser = TestDataBuilder.CreateUser(email: "existing@test.com");
            _context.Users.Add(existingUser);
            await _context.SaveChangesAsync();

            var registerDto = new RegisterDto
            {
                Email = "existing@test.com",
                Password = "Password123!",
                FullName = "New User"
            };

            // Act
            var act = async () => await _authService.RegisterAsync(registerDto);

            // Assert
            await act.Should().ThrowAsync<ConflictException>()
                .WithMessage("*email already exists*");
        }

        [Fact]
        public async Task RegisterAsync_EmailIsCaseInsensitive_ThrowsConflictException()
        {
            // Arrange
            var existingUser = TestDataBuilder.CreateUser(email: "user@test.com");
            _context.Users.Add(existingUser);
            await _context.SaveChangesAsync();

            var registerDto = new RegisterDto
            {
                Email = "USER@TEST.COM",
                Password = "Password123!",
                FullName = "New User"
            };

            // Act
            var act = async () => await _authService.RegisterAsync(registerDto);

            // Assert
            await act.Should().ThrowAsync<ConflictException>();
        }

        [Fact]
        public async Task RegisterAsync_PasswordIsHashed_NotStoredPlainText()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Email = "hashtest@test.com",
                Password = "Password123!",
                FullName = "Hash Test User"
            };

            // Act
            await _authService.RegisterAsync(registerDto);

            // Assert
            var user = await _context.Users.FindAsync(1);
            user.Should().NotBeNull();
            user!.PasswordHash.Should().NotBe("Password123!");
            user.PasswordHash.Should().StartWith("$2"); // BCrypt hash prefix
        }

        [Fact]
        public async Task RegisterAsync_NewUserHasUserRole()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Email = "roletest@test.com",
                Password = "Password123!",
                FullName = "Role Test User"
            };

            // Act
            var result = await _authService.RegisterAsync(registerDto);

            // Assert
            result.Role.Should().Be(UserRole.User);
        }

        #endregion

        #region Login Tests

        [Fact]
        public async Task LoginAsync_ValidCredentials_ReturnsAuthResponse()
        {
            // Arrange
            var password = "Password123!";
            var user = new User
            {
                Email = "login@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                FullName = "Login User",
                Role = UserRole.User,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var loginDto = new LoginDto
            {
                Email = "login@test.com",
                Password = password
            };

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            result.Should().NotBeNull();
            result.Token.Should().NotBeNullOrEmpty();
            result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
            result.User.Should().NotBeNull();
            result.User.Email.Should().Be("login@test.com");
        }

        [Fact]
        public async Task LoginAsync_InvalidEmail_ThrowsForbiddenException()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "nonexistent@test.com",
                Password = "Password123!"
            };

            // Act
            var act = async () => await _authService.LoginAsync(loginDto);

            // Assert
            await act.Should().ThrowAsync<ForbiddenException>()
                .WithMessage("*Invalid email or password*");
        }

        [Fact]
        public async Task LoginAsync_WrongPassword_ThrowsForbiddenException()
        {
            // Arrange
            var user = new User
            {
                Email = "wrongpass@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword123!"),
                FullName = "Wrong Pass User",
                Role = UserRole.User,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var loginDto = new LoginDto
            {
                Email = "wrongpass@test.com",
                Password = "WrongPassword123!"
            };

            // Act
            var act = async () => await _authService.LoginAsync(loginDto);

            // Assert
            await act.Should().ThrowAsync<ForbiddenException>()
                .WithMessage("*Invalid email or password*");
        }

        [Fact]
        public async Task LoginAsync_EmailIsCaseInsensitive_LoginSucceeds()
        {
            // Arrange
            var password = "Password123!";
            var user = new User
            {
                Email = "casetest@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                FullName = "Case Test User",
                Role = UserRole.User,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var loginDto = new LoginDto
            {
                Email = "CASETEST@TEST.COM",
                Password = password
            };

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            result.Should().NotBeNull();
            result.Token.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task LoginAsync_JwtTokenContainsCorrectClaims()
        {
            // Arrange
            var password = "Password123!";
            var user = new User
            {
                Email = "claims@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                FullName = "Claims User",
                Role = UserRole.Admin,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var loginDto = new LoginDto
            {
                Email = "claims@test.com",
                Password = password
            };

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(result.Token);

            token.Claims.Should().Contain(c => c.Type == "email" && c.Value == "claims@test.com");
            token.Claims.Should().Contain(c => c.Type == System.Security.Claims.ClaimTypes.Role && c.Value == "Admin");
        }

        #endregion

        #region GetUser Tests

        [Fact]
        public async Task GetUserByIdAsync_ExistingUser_ReturnsUser()
        {
            // Arrange
            var user = TestDataBuilder.CreateUser(id: 1, email: "byid@test.com");
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _authService.GetUserByIdAsync(1);

            // Assert
            result.Should().NotBeNull();
            result!.Email.Should().Be("byid@test.com");
        }

        [Fact]
        public async Task GetUserByIdAsync_NonExistentUser_ReturnsNull()
        {
            // Act
            var result = await _authService.GetUserByIdAsync(999);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetUserByEmailAsync_ExistingUser_ReturnsUser()
        {
            // Arrange
            var user = TestDataBuilder.CreateUser(id: 1, email: "byemail@test.com");
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _authService.GetUserByEmailAsync("byemail@test.com");

            // Assert
            result.Should().NotBeNull();
            result!.Email.Should().Be("byemail@test.com");
        }

        [Fact]
        public async Task GetUserByEmailAsync_NonExistentUser_ReturnsNull()
        {
            // Act
            var result = await _authService.GetUserByEmailAsync("nonexistent@test.com");

            // Assert
            result.Should().BeNull();
        }

        #endregion
    }
}
