using Microsoft.EntityFrameworkCore;
using MoviesTelegramBot.Configurations;
using MoviesTelegramBot.Models;
namespace MoviesTelegramBot.Data;


public class AppDbContext: DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<Users> Users { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        base.OnModelCreating(modelBuilder);
    }
    
}