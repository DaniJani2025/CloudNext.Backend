using CloudNext.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CloudNext.Utils
{
    public static class JwtTokenHelper
    {
        private const int TOKEN_EXPIRATION_HOURS = 24;
        private const int REFRESH_TOKEN_EXPIRATION_DAYS = 7;

        private static SymmetricSecurityKey GetSecurityKey(IConfiguration configuration)
        {
            var key = configuration["JWT_SECRET_KEY"]
                      ?? throw new InvalidOperationException("JWT Key not configured");
            return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        }

        public static string GenerateJwtToken(User user, IConfiguration configuration)
        {
            var key = GetSecurityKey(configuration);
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var token = new JwtSecurityToken(
                issuer: configuration["Jwt:Issuer"],
                audience: configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(TOKEN_EXPIRATION_HOURS),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public static string GenerateRefreshToken(User user, IConfiguration configuration)
        {
            var key = GetSecurityKey(configuration);
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.SerialNumber, Guid.NewGuid().ToString())
            };

            var refreshToken = new JwtSecurityToken(
                issuer: configuration["Jwt:Issuer"],
                audience: configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(REFRESH_TOKEN_EXPIRATION_DAYS),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(refreshToken);
        }

        public static string GenerateRegistrationToken(string email, IConfiguration configuration)
        {
            var key = GetSecurityKey(configuration);
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.SerialNumber, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: configuration["Jwt:Issuer"],
                audience: configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(TOKEN_EXPIRATION_HOURS),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public static string ValidateRegistrationToken(string token, IConfiguration configuration)
        {
            var key = GetSecurityKey(configuration);
            var tokenHandler = new JwtSecurityTokenHandler();

            try
            {
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    RequireExpirationTime = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
                return principal.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
            }
            catch
            {
                return null!;
            }
        }
    }
}
