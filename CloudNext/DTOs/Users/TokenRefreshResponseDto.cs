namespace CloudNext.DTOs.Users
{
    public class TokenRefreshResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }
}
