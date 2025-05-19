namespace CloudNext.DTOs.Users
{
    public class ResetPasswordResponseDto
    {
        public string Message { get; set; } = string.Empty;
        public string NewRecoveryKey { get; set; } = string.Empty;
    }

}
