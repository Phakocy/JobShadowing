using JobShadowing.Interfaces;
using JobShadowing.Models.Dtos;
using JobShadowing.Models.Dtos.Auth;
using Microsoft.AspNetCore.Mvc;

namespace JobShadowing.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("register")]
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

        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<AuthResponseDto>> Login(LoginDto loginDto)
        {
            _logger.LogInformation("Login attempt for email: {Email}", loginDto.Email);

            var authResponse = await _authService.LoginAsync(loginDto);

            _logger.LogInformation("User logged in successfully: {Email}", loginDto.Email);

            return Ok(authResponse);
        }
    }
}
