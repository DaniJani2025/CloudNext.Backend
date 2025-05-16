using CloudNext.Models;

namespace CloudNext.DTOs.Users
{
    public class RegisterResult
    {
        public User User { get; set; } = default!;
        public string RecoveryKey { get; set; } = string.Empty;
    }
}
