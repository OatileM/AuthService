using Microsoft.AspNetCore.Identity;

namespace AuthService.Services
{
    public interface ITokenService
    {
        Task<string> GenerateToken(IdentityUser user);
    }
}
