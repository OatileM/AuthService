using Microsoft.EntityFrameworkCore;
using System.Data;
using AuthService.Models;

namespace AuthService.Data
{
    public class MyDbContext: DbContext
    {

        public MyDbContext(DbContextOptions<MyDbContext> options)
       : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
    }
}
