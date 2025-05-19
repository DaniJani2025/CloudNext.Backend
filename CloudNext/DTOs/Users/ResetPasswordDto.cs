namespace CloudNext.DTOs.Users
{
    public class ResetPasswordDto
    {
        public string Token { get; set; }
        public string NewPassword { get; set; }
        public string RecoveryKey { get; set; }
    }
}
