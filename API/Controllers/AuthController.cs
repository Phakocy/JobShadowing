using JobShadowing.Application.DTOs;
using JobShadowing.Application.DTOs.Auth;
using JobShadowing.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace JobShadowing.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    [Tags("Authentication")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        // Register a new user account
        /// <param name="registerDto">User registration details</param>
        // <returns>The created user information</returns>
        [HttpPost("register")]
        [SwaggerOperation(Summary = "Register a new user", Description = "Creates a new user account with the provided credentials")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
        public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
        {
            _logger.LogInformation("Registration attempt for email: {Email}", registerDto.Email);

            var userDto = await _authService.RegisterAsync(registerDto);

            _logger.LogInformation("User registered successfully: {Email}", registerDto.Email);

            return CreatedAtAction(nameof(Register), userDto);
        }

        // Login to get a JWT token
        /// <param name="loginDto">User login credentials</param>
        // <returns>JWT token and user information</returns>
        [HttpPost("login")]
        [SwaggerOperation(Summary = "Login and get JWT token", Description = "Authenticates user and returns a JWT token for API access")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<AuthResponseDto>> Login(LoginDto loginDto)
        {
            _logger.LogInformation("Login attempt for email: {Email}", loginDto.Email);

            var authResponse = await _authService.LoginAsync(loginDto);

            _logger.LogInformation("User logged in successfully: {Email}", loginDto.Email);

            return Ok(authResponse);
        }
    }
}
