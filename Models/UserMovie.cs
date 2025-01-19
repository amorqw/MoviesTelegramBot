using Telegram.Bot.Types;

namespace MoviesTelegramBot.Models;

public class UserMovie
{
    public long UserId { get; set; }
    public User User { get; set; }

    public int MovieId { get; set; }
    public Movie Movie { get; set; }
}