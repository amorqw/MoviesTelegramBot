using Microsoft.EntityFrameworkCore;
using MoviesTelegramBot.Data; 
using MoviesTelegramBot.Interfaces;
using MoviesTelegramBot.Models; 
using Telegram.Bot.Types;
using User = MoviesTelegramBot.Models.User;

namespace MoviesTelegramBot.Services
{
    public class UserService : IUser
    {
        private readonly AppDbContext _context;

        public UserService(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddMovie(long userId, string movieName)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                user = new User { UserId = userId };
                _context.Users.Add(user);
            }

            var movie = await _context.Movies.FirstOrDefaultAsync(m => m.MovieName == movieName);
            if (movie == null)
            {
                movie = new Movie { MovieName = movieName };
                _context.Movies.Add(movie);
                await _context.SaveChangesAsync(); 
            }

            
            if (!await _context.UserMovies.AnyAsync(um => um.UserId == userId && um.MovieId == movie.MovieId))
            {
                var userMovie = new UserMovie { UserId = userId, MovieId = movie.MovieId };
                _context.UserMovies.Add(userMovie);
                await _context.SaveChangesAsync(); 
            }

        }

        public async Task RemoveMovie(long userId, int movieId)
        {
            var userMovie = await _context.UserMovies
                .FirstOrDefaultAsync(um => um.UserId == userId && um.MovieId == movieId);

            if (userMovie != null)
            {
                _context.UserMovies.Remove(userMovie);
                await _context.SaveChangesAsync();

                if (!await _context.UserMovies.AnyAsync(um => um.MovieId == movieId))
                {
                    var movieToRemove = await _context.Movies.FindAsync(movieId);
                    if (movieToRemove != null)
                    {
                        _context.Movies.Remove(movieToRemove);
                        await _context.SaveChangesAsync();
                    }
                }
            }
        }

        public async Task<List<string>> GetAllMoviesFromUser(long userId)
        {
            var userMovies = await _context.UserMovies
                .Where(um => um.UserId == userId)
                .Select(um => um.Movie.MovieName) 
                .ToListAsync();

            return userMovies;
        }
    }
}