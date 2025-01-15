using MoviesTelegramBot.Models;

namespace MoviesTelegramBot.Interfaces;

public interface IUser
{
    Task AddMovie(long userId, string movie);
    Task RemoveMovie(long userId, long movieId);
    Task<List<string>> GetAllMoviesFromUser(long userId);
}