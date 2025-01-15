using Microsoft.EntityFrameworkCore;
using MoviesTelegramBot.Data;
using MoviesTelegramBot.Interfaces;
using MoviesTelegramBot.Models;

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
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null)
        {
            user = new Users
            {
                UserId = userId,
                Movie = movie
            };
            _context.Users.Add(user);
        }
        else
        {
            user.UserId = userId;
            user.Movie = movie; 
        }
        await _context.SaveChangesAsync();
    }

    public async Task RemoveMovie(long userId, long movie)
    {
        var user = await _context.Users.FindAsync(userId, movie);
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
    }

    public async Task<List<string>> GetAllMoviesFromUser(long userId)
    {
        return await _context.Users.Where(u => u.UserId == userId).Select(u => u.Movie).ToListAsync();
    }
    
}