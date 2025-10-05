using Microsoft.EntityFrameworkCore;
using WondersAPI.Models;

namespace WondersAPI.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Wonder> Wonders => Set<Wonder>();

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    }
}