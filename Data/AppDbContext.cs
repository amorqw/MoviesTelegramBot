using Microsoft.EntityFrameworkCore;
using MoviesTelegramBot.Configurations;
using MoviesTelegramBot.Models;
namespace MoviesTelegramBot.Data;


public class AppDbContext: DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<User> Users { get; set; }
    public DbSet<Movie> Movies { get; set; }
    public DbSet<UserMovie> UserMovies { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new MovieConfiguration());
        modelBuilder.ApplyConfiguration(new UserMovieConfiguration());
        base.OnModelCreating(modelBuilder);
    }
    
}