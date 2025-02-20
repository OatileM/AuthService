using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AuthService.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        private readonly UserManager<User> _userManager;  // Changed to User

        public TokenService(IConfiguration configuration, UserManager<User> userManager)  // Changed to User
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        public async Task<string> GenerateToken(IdentityUser identityUser)
        {
            if (identityUser == null)
                throw new ArgumentNullException(nameof(identityUser));

            // Cast the IdentityUser to User
            var user = identityUser as User;
            if (user == null)
                throw new ArgumentException("Invalid user type", nameof(identityUser));

            var jwtKey = _configuration["Jwt:Key"] ??
                throw new ArgumentNullException("Jwt:Key", "JWT Key configuration is missing");
            var issuer = _configuration["Jwt:Issuer"] ??
                throw new ArgumentNullException("Jwt:Issuer", "JWT Issuer configuration is missing");
            var audience = _configuration["Jwt:Audience"] ??
                throw new ArgumentNullException("Jwt:Audience", "JWT Audience configuration is missing");

            try
            {
                var userRoles = await _userManager.GetRolesAsync(user);  // Using user instead of identityUser

                // Create claims
                var claims = new[]
                {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id)
            }.Union(userRoles.Select(role => new Claim(ClaimTypes.Role, role)));

                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
                var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
                var now = DateTime.UtcNow;

                var token = new JwtSecurityToken(
                    issuer: issuer,
                    audience: audience,
                    claims: claims,
                    notBefore: now,
                    expires: now.AddMinutes(30),
                    signingCredentials: credentials
                );

                return new JwtSecurityTokenHandler().WriteToken(token);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to retrieve user roles", ex);
            }
        }
    }
}
