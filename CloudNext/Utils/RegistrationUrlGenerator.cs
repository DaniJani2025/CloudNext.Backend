using Microsoft.Extensions.Configuration;

namespace CloudNext.Utils
{
    public static class RegistrationUrlGenerator
    {
        public static string GenerateRegistrationUrl(string email, IConfiguration configuration)
        {
            string token = JwtTokenHelper.GenerateRegistrationToken(email, configuration);
            string baseUrl = configuration["AppSettings:RegistrationBaseUrl"]
                ?? throw new InvalidOperationException("Registration base URL is not configured.");

            return $"{baseUrl}/api/users/verify?token={token}";
        }
    }
}
