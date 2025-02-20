using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using AuthService.Models;  // Add this for your custom Role class

namespace AuthService.Data
{
    public static class RoleInitializer
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<Role>>();  // Change to Role

            string[] roleNames = { "Admin", "User" };

            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    var role = new Role(roleName)  // Use your custom Role class
                    {
                        Description = $"Default {roleName} role",
                        CreatedAt = DateTime.UtcNow
                    };
                    await roleManager.CreateAsync(role);
                }
            }
        }
    }
}
