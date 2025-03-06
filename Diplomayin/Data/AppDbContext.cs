using Diplomayin.Models;
using Microsoft.EntityFrameworkCore;

namespace Diplomayin.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Device> Devices { get; set; }
        public DbSet<Policy> Policies { get; set; }

         public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }
    }
}
