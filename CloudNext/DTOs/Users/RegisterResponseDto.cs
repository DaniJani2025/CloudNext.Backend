namespace CloudNext.DTOs.Users
{
    public class RegisterResponseDto
    {
        public Guid UserId { get; set; }
        public required string Email { get; set; }
        public required string Message { get; set; }
        public string RecoveryKey { get; set; } = string.Empty;
    }
}
