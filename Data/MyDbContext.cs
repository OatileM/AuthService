using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System.Data;
using AuthService.Models;
using Microsoft.AspNetCore.Identity;

namespace AuthService.Data
{
    public class MyDbContext: IdentityDbContext<IdentityUser>
    {

        public MyDbContext(DbContextOptions<MyDbContext> options)
       : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
    }
}
