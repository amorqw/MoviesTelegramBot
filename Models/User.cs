namespace MoviesTelegramBot.Models;

public class User
{
    public long UserId { get; set; }
    public List<UserMovie> UserMovies { get; set; } = new(); 
}