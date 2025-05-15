using CloudNext.DTOs.Users;
using CloudNext.Models;
using CloudNext.Utils;
using CloudNext.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;
using CloudNext.Interfaces;
using System.Security.Claims;
using System.Text;

namespace CloudNext.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IUserFolderRepository _userFolderRepository;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUserSessionService _userSessionService;
        private readonly SMTPService _smtpService;

        public UserService
        (
            IUserRepository userRepository,
            IUserFolderRepository userFolderRepository,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            IUserSessionService userSessionService,
            SMTPService smtpService
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

            var derivedKey = EncryptionHelper.DeriveKeyFromPassword(password, user.PasswordSalt!);

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
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(Constants.Token.RefreshExpirationDays);
            await _userRepository.UpdateUserAsync(user);

            var httpContext = _httpContextAccessor.HttpContext;
            httpContext?.Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(Constants.Token.RefreshExpirationDays)
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
            Console.WriteLine($"Encryption key: {encryptionKey}");

            var saltBytes = new byte[16];
            RandomNumberGenerator.Fill(saltBytes);
            var saltHex = Convert.ToHexString(saltBytes);

            var derivedKey = EncryptionHelper.DeriveKeyFromPassword(password, saltHex);
            var encryptedUserKey = EncryptionHelper.EncryptData(encryptionKey, derivedKey);

            // Generate recovery key without hex encoding
            var recoveryKey = GeneratorHelper.GenerateRecoveryKey(_configuration);
            Console.WriteLine($"Recovery key: {recoveryKey}");
            var recoveryKeyHex = Convert.ToHexString(Encoding.UTF8.GetBytes(recoveryKey));
            Console.WriteLine($"Recovery key hex: {recoveryKeyHex}");

            // Encrypt the recovery key directly (no hex encoding)
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

            Console.WriteLine($"Verification Url: {verificationURL}");

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

        public async Task<string?> VerifyEmailAsync(string token)
        {
            var email = JwtTokenHelper.ValidateRegistrationToken(token, _configuration);
            if (string.IsNullOrEmpty(email))
                return null;

            var user = await _userRepository.GetUserByEmailAsync(email);

            if (user == null || user.IsVerified)
                return null;

            user.IsVerified = true;
            await _userRepository.UpdateUserAsync(user);

            var AppBaseUrl = _configuration["AppSettings:AppBaseUrl"];
            return $"{AppBaseUrl}/verification-complete";
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
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(Constants.Token.RefreshExpirationDays);
            await _userRepository.UpdateUserAsync(user);

            var httpContext = _httpContextAccessor.HttpContext;
            httpContext?.Response.Cookies.Append("refreshToken", newRefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(Constants.Token.RefreshExpirationDays)
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

        public async Task<string> RequestPasswordResetAsync(string email)
        {
            var user = await _userRepository.GetUserByEmailAsync(email);
            if (user == null)
                return "User with this email doesn't exist.";

            var resetToken = JwtTokenHelper.GeneratePasswordResetToken(user, _configuration);
            var resetUrl = $"https://localhost:5173/reset-password?token={resetToken}";
            Console.WriteLine($"Reset Url: {resetUrl}");

            await _smtpService.SendPasswordResetMailAsync(email, resetUrl);

            return "Password reset email sent.";
        }

        public async Task<string> ResetPasswordAsync(string token, string newPassword, string suppliedRecoveryKey)
        {
            // 1. Validate the JWT and load the user
            var principal = JwtTokenHelper.ValidateResetPasswordToken(token, _configuration);
            if (principal == null)
                return "Invalid or expired reset token.";

            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return "Invalid token claims.";

            var userId = Guid.Parse(userIdClaim);
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
                return "User not found.";

            // 2. Recover the stored recovery key by decrypting with your RootKey
            var rootKey = _configuration["Security:RootKey"]
                           ?? throw new InvalidOperationException("Root key is missing.");
            var storedRecoveryKey = EncryptionHelper.DecryptData(user.EncryptedRecoveryKey!, rootKey);

            // 2. Verify the supplied recovery key
            if (suppliedRecoveryKey != storedRecoveryKey)
                return "Recovery key is incorrect.";

            // 3. Convert that plain-text recovery key into a hex string so it works with DecryptData
            var recoveryKeyBytes = Encoding.UTF8.GetBytes(storedRecoveryKey);
            var recoveryKeyHex = Convert.ToHexString(recoveryKeyBytes);

            // 4. Now decrypt your encrypted-user-key blob using that hex-encoded key
            var originalEncKey = EncryptionHelper.DecryptData(
                user.RecoveryEncryptedUserKey!,
                recoveryKeyHex
            );

            // 5. Derive a new key from the new password + existing salt
            var saltHex = user.PasswordSalt!;
            var derivedKey = EncryptionHelper.DeriveKeyFromPassword(newPassword, saltHex);

            // 6. Re-encrypt the same originalEncKey with the new password-derived key
            user.EncryptedUserKey = EncryptionHelper.EncryptData(originalEncKey, derivedKey);

            // 7. Rotate the recovery key the same way you do in Register:
            var newRecoveryKey = GeneratorHelper.GenerateRecoveryKey(_configuration);
            var newRecoveryKeyHexBytes = Encoding.UTF8.GetBytes(newRecoveryKey);
            var newRecoveryKeyHex = Convert.ToHexString(newRecoveryKeyHexBytes);

            //   a) store encryptionKey encrypted under the new recovery key
            user.RecoveryEncryptedUserKey
                = EncryptionHelper.EncryptData(originalEncKey, newRecoveryKeyHex);

            //   b) store new recovery key encrypted under the root key
            user.EncryptedRecoveryKey
                = EncryptionHelper.EncryptData(newRecoveryKey, rootKey);

            // 8. Finally, hash + update the password
            user.PasswordHash = HashPassword(newPassword);
            // (keep saltHex the same)

            await _userRepository.UpdateUserAsync(user);
            return "Password reset successfully.";
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
