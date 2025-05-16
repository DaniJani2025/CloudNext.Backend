using CloudNext.DTOs.Users;
using CloudNext.Models;

namespace CloudNext.Interfaces
{
    public interface IUserService
    {
        Task<(User?, string Token, string Message)> AuthenticateUserAsync(string email, string password);
        Task<RegisterResult?> RegisterUserAsync(string email, string password);
        Task<string?> VerifyEmailAsync(string token);
        Task<(string? AccessToken, bool Success, string Message)> RefreshTokensAsync(string refreshToken);
        void Logout(Guid userId);
        Task<string> RequestPasswordResetAsync(string email);
        Task<ResetPasswordResult> ResetPasswordAsync(string token, string newPassword, string suppliedRecoveryKey);
        Task<bool> UpdateUserEmailAsync(Guid userId, string newEmail);
        Task<bool> DeleteUserAsync(Guid userId);
    }
}
