using System.Security.Claims;
using CloudNext.DTOs;
using CloudNext.DTOs.Users;
using CloudNext.Models;
using CloudNext.Services;
using Microsoft.AspNetCore.Authorization;
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

        [Authorize]
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized(ApiResponse<string>.ErrorResponse("User not authenticated"));

            var userId = Guid.Parse(userIdClaim.Value);

            _userService.Logout(userId);

            Response.Cookies.Delete("refreshToken");

            return Ok(ApiResponse<string>.SuccessResponse("Logged out successfully"));
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
            var redirectUrl = await _userService.VerifyEmailAsync(token);

            if (string.IsNullOrEmpty(redirectUrl))
                return BadRequest(ApiResponse<string>.ErrorResponse("Invalid or expired token"));

            return Redirect(redirectUrl);
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
