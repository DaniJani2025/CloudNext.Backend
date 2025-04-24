using CloudNext.DTOs.Users;
using CloudNext.Models;
using CloudNext.Repositories.Users;
using CloudNext.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;
using CloudNext.Interfaces;
using System.Security.Claims;
using System.Text;

namespace CloudNext.Services
{
    public class UserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IUserFolderRepository _userFolderRepository;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly SMTPService _smtpService;
        private readonly UserSessionService _userSessionService;

        public UserService
        (
            IUserRepository userRepository,
            IUserFolderRepository userFolderRepository,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            SMTPService smtpService,
            UserSessionService userSessionService
        )
        {
            _userRepository = userRepository;
            _userFolderRepository = userFolderRepository;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _smtpService = smtpService;
            _userSessionService = userSessionService;
        }

        public async Task<(User?, string Token, string Message)> AuthenticateUserAsync(string email, string password)
        {
            var user = await _userRepository.GetUserByEmailAsync(email);
            if (user == null || !VerifyPassword(password, user.PasswordHash))
                return (null, string.Empty, "Invalid email or password");

            if (!user.IsVerified)
                return (null, string.Empty, "User is not verified yet");

            var derivedKey = GeneratorHelper.DeriveKeyFromPassword(password, user.PasswordSalt!);

            string decryptedUserKey;
            try
            {
                decryptedUserKey = EncryptionHelper.DecryptData(user.EncryptedUserKey!, derivedKey);
            }
            catch
            {
                return (null, string.Empty, "Failed to decrypt user encryption key. Invalid password or corrupted data.");
            }

            _userSessionService.SetSession(user.Id, decryptedUserKey);

            var token = JwtTokenHelper.GenerateJwtToken(user, _configuration);
            var refreshToken = JwtTokenHelper.GenerateRefreshToken(user, _configuration);

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _userRepository.UpdateUserAsync(user);

            var httpContext = _httpContextAccessor.HttpContext;
            httpContext?.Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(7)
            });

            return (user, token, "Login successful");
        }
        
        public async Task<User?> RegisterUserAsync(string email, string password)
        {
            if (await _userRepository.GetUserByEmailAsync(email) != null)
                return null;

            Guid userId = Guid.NewGuid();

            var registrationToken = JwtTokenHelper.GenerateRegistrationToken(email, _configuration);
            var verificationURL = GeneratorHelper.GenerateRegistrationUrl(email, _configuration);

            var encryptionKey = GeneratorHelper.GenerateEncryptionKey(_configuration);

            var saltBytes = new byte[16];
            RandomNumberGenerator.Fill(saltBytes);
            var saltHex = Convert.ToHexString(saltBytes);

            var derivedKey = GeneratorHelper.DeriveKeyFromPassword(password, saltHex);
            var encryptedUserKey = EncryptionHelper.EncryptData(encryptionKey, derivedKey);

            var recoveryKey = GeneratorHelper.GenerateRecoveryKey(_configuration);

            var recoveryKeyHex = Convert.ToHexString(Encoding.UTF8.GetBytes(recoveryKey));
            var recoveryEncryptedUserKey = EncryptionHelper.EncryptData(encryptionKey, recoveryKeyHex);

            var rootKey = _configuration["Security:RootKey"] ?? throw new InvalidOperationException("Root key is missing.");
            var encryptedRecoveryKey = EncryptionHelper.EncryptData(recoveryKey, rootKey);

            var newUser = new User
            {
                Id = userId,
                Email = email,
                PasswordHash = HashPassword(password),
                RegistrationToken = registrationToken,
                IsVerified = false,
                PasswordSalt = saltHex,
                EncryptedUserKey = encryptedUserKey,
                EncryptedRecoveryKey = encryptedRecoveryKey,
                RecoveryEncryptedUserKey = recoveryEncryptedUserKey
            };

            _userSessionService.SetSession(userId, encryptionKey);
            await _userRepository.AddUserAsync(newUser);
            await _smtpService.SendRegistrationMailAsync(email, verificationURL);

            var rootFolder = await _userFolderRepository.GetFolderAsync(userId, null, "root");

            if (rootFolder != null)
                throw new InvalidOperationException("Root folder already exists for this user.");

            var folderPath = Path.Combine(AppContext.BaseDirectory, "Documents", userId.ToString());
            Directory.CreateDirectory(folderPath);

            var newUserFolder = new UserFolder
            {
                Name = userId.ToString(),
                UserId = userId,
                ParentFolderId = null,
                VirtualPath = ""
            };

            await _userFolderRepository.AddFolderAsync(newUserFolder);

            return newUser;
        }

        public async Task<bool> VerifyEmailAsync(string token)
        {
            var email = JwtTokenHelper.ValidateRegistrationToken(token, _configuration);
            if (string.IsNullOrEmpty(email))
                return false;

            var user = await _userRepository.GetUserByEmailAsync(email);

            if (user == null || user.IsVerified)
                return false;

            user.IsVerified = true;
            await _userRepository.UpdateUserAsync(user);

            return true;
        }

        public async Task<(string? AccessToken, bool Success, string Message)> RefreshTokensAsync(string refreshToken)
        {
            var principal = JwtTokenHelper.ValidateRefreshToken(refreshToken, _configuration);
            if (principal == null)
                return (null, false, "Invalid refresh token");

            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return (null, false, "Invalid token claims");

            var userId = Guid.Parse(userIdClaim.Value);
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime < DateTime.UtcNow)
                return (null, false, "Refresh token is invalid or expired");

            var newAccessToken = JwtTokenHelper.GenerateJwtToken(user, _configuration);
            var newRefreshToken = JwtTokenHelper.GenerateRefreshToken(user, _configuration);

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _userRepository.UpdateUserAsync(user);

            var httpContext = _httpContextAccessor.HttpContext;
            httpContext?.Response.Cookies.Append("refreshToken", newRefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(7)
            });

            return (newAccessToken, true, "Tokens refreshed successfully");
        }

        public void Logout(Guid userId)
        {
            _userSessionService.RemoveSession(userId);
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
