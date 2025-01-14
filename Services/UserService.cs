using MoviesTelegramBot.Data;
using MoviesTelegramBot.Interfaces;
using MoviesTelegramBot.Models;
using Telegram.Bot.Types;

namespace MoviesTelegramBot.Services;

public class UserService: IUser
{
    private readonly AppDbContext _context;

    public UserService(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddMovie(long userId, string movie)
    {
        var newUser = new Users
        {
            UserId = userId,
            Movie = movie
        };
        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveMovie(long userId, long movie)
    {
        var user = await _context.Users.FindAsync(userId, movie);
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
    }

    public List<Users> GetAllMoviesFromUser(long userId)
    {
        return  _context.Users.Where(u => u.UserId == userId).ToList();
    }
    
}