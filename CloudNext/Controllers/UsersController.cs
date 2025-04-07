using CloudNext.DTOs;
using CloudNext.DTOs.Users;
using CloudNext.Models;
using CloudNext.Services;
using Microsoft.AspNetCore.Mvc;

namespace CloudNext.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly UserService _userService;

        public UsersController(UserService userService)
        {
            _userService = userService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            var (user, token, message) = await _userService.AuthenticateUserAsync(request.Email, request.Password);

            if (user == null)
                return Unauthorized(ApiResponse<LoginResponseDto>.ErrorResponse(message));

            var response = new LoginResponseDto
            {
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                UserId = user.Id,
                Email = user.Email
            };

            return Ok(ApiResponse<LoginResponseDto>.SuccessResponse(response));
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            var user = await _userService.RegisterUserAsync(request.Email, request.Password);

            if (user == null)
                return Conflict(ApiResponse<RegisterResponseDto>.ErrorResponse("User already exists"));

            var response = new RegisterResponseDto
            {
                UserId = user.Id,
                Email = user.Email,
                Message = "User registered successfully"
            };

            return Ok(ApiResponse<RegisterResponseDto>.SuccessResponse(response));
        }

        [HttpGet("verify")]
        public async Task<IActionResult> VerifyEmail([FromQuery] string token)
        {
            var result = await _userService.VerifyEmailAsync(token);

            if (!result)
                return BadRequest(ApiResponse<string>.ErrorResponse("Invalid or expired token"));

            return Ok(ApiResponse<string>.SuccessResponse("Email verified successfully"));
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken()
        {
            var refreshToken = HttpContext.Request.Cookies["refreshToken"];
            if (string.IsNullOrEmpty(refreshToken))
                return Unauthorized(ApiResponse<string>.ErrorResponse("No refresh token provided."));

            var (newAccessToken, success, message) = await _userService.RefreshTokensAsync(refreshToken);
            if (!success || newAccessToken == null)
                return Unauthorized(ApiResponse<string>.ErrorResponse(message));

            var response = new TokenRefreshResponseDto
            {
                Token = newAccessToken,
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };

            return Ok(ApiResponse<TokenRefreshResponseDto>.SuccessResponse(response));
        }
    }
}
