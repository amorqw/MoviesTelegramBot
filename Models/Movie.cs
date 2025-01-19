namespace MoviesTelegramBot.Models;

public class Movie
{
    public int MovieId { get; set; }
    public string MovieName { get; set; }
    public List<UserMovie> UserMovies { get; set; } = new(); // Навигационное свойство
}