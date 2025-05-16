namespace CloudNext.DTOs.Users
{
    public class ResetPasswordResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string NewRecoveryKey { get; set; } = string.Empty;
    }
}
