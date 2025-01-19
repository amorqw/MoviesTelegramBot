using MoviesTelegramBot.Models;

namespace MoviesTelegramBot.Interfaces;

public interface IUser
{
    Task AddMovie(long userId, string movie);
    Task RemoveMovie(long userId, int movieId);
    Task<List<string>> GetAllMoviesFromUser(long userId);
}