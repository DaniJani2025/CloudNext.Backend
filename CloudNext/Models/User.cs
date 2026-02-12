namespace CloudNext.Models
{
    public class User : BaseEntity
    {
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
        public DateTime? DeactivatedAt { get; set; }
        public DateTime? ScheduledDeletionAt { get; set; }

        public List<UserFolder> Folders { get; set; } = new();
        public List<UserFile> Files { get; set; } = new();

        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }

        public string? RegistrationToken { get; set; }
        public bool IsVerified { get; set; } = false;

        public string? EncryptedUserKey { get; set; }
        public string? EncryptedRecoveryKey { get; set; }
        public string? PasswordSalt { get; set; }
        public string? RecoveryEncryptedUserKey { get; set; }

    }
}
