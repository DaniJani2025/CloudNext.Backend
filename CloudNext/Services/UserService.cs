using CloudNext.DTOs.Users;
using CloudNext.Models;
using CloudNext.Repositories.Users;
using CloudNext.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;

namespace CloudNext.Services
{
    public class UserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly SMTPService _smtpService;

        public UserService(IUserRepository userRepository, IConfiguration configuration, IHttpContextAccessor httpContextAccessor, SMTPService smtpService)
        {
            _userRepository = userRepository;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _smtpService = smtpService;
        }

        public async Task<(User?, string Token)> AuthenticateUserAsync(string email, string password)
        {
            var user = await _userRepository.GetUserByEmailAsync(email);
            if (user == null || !VerifyPassword(password, user.PasswordHash))
                return (null, string.Empty);

            var token = JwtTokenHelper.GenerateJwtToken(user, _configuration);
            var refreshToken = JwtTokenHelper.GenerateRefreshToken(user, _configuration);

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _userRepository.UpdateUserAsync(user);

            // Store refresh token in a secure HTTP-only cookie
            var httpContext = _httpContextAccessor.HttpContext;
            httpContext?.Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(7)
            });

            return (user, token);
        }

        public async Task<User?> RegisterUserAsync(string email, string password)
        {
            if (await _userRepository.GetUserByEmailAsync(email) != null)
                return null; // User already exists

            var registrationToken = JwtTokenHelper.GenerateRegistrationToken(email, _configuration);

            var newUser = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                PasswordHash = HashPassword(password),
                RegistrationToken = registrationToken,
                IsVerified = false // Not verified yet
            };

            await _userRepository.AddUserAsync(newUser);

            await _smtpService.SendRegistrationMailAsync(email, registrationToken);

            return newUser;
        }


        public async Task<bool> UpdateUserEmailAsync(Guid userId, string newEmail)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null) return false;

            user.Email = newEmail;
            await _userRepository.UpdateUserAsync(user);
            return true;
        }

        public async Task<bool> DeleteUserAsync(Guid userId)
        {
            await _userRepository.DeleteUserAsync(userId);
            return true;
        }

        private bool VerifyPassword(string password, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }

        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }
    }
}
