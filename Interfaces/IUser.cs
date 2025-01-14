using MoviesTelegramBot.Models;

namespace MoviesTelegramBot.Interfaces;

public interface IUser
{
    Task AddMovie(long userId, string movie);
    Task RemoveMovie(long userId, long movieId);
    List<Users> GetAllMoviesFromUser(long userId);
}