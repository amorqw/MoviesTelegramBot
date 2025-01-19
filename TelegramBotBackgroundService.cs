
using System.Reflection.Metadata;
using Microsoft.EntityFrameworkCore;
using MoviesTelegramBot.Data;
using MoviesTelegramBot.Features;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using MoviesTelegramBot.Interfaces;
using Telegram.Bot.Types.ReplyMarkups;

namespace MoviesTelegramBot
{
    public class TelegramBotBackgroundService : IHostedService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<TelegramBotBackgroundService> _logger;
        private CancellationTokenSource _cts;
        private IHandler<Message> _messageHandler;
        public TelegramBotBackgroundService(
            ITelegramBotClient botClient,
            IServiceScopeFactory scopeFactory,
            ILogger<TelegramBotBackgroundService> logger,
            IHandler<Message> messageHandler)
        {
            _botClient = botClient;
            _scopeFactory = scopeFactory;
            _logger = logger;
            _messageHandler = messageHandler;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            ReceiverOptions receiverOptions = new()
            {
                AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery }
            };

            _botClient.StartReceiving(
                updateHandler: HandleUpdateAsync,
                errorHandler: HandleErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: _cts.Token);

            _logger.LogInformation("Telegram bot started receiving updates.");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _cts?.Cancel();
            _logger.LogInformation("Telegram bot is stopping...");
            return Task.CompletedTask;
        }


private readonly Dictionary<long, string> _userStates = new(); 

private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    using var scope = _scopeFactory.CreateScope();
    var messageHandler = scope.ServiceProvider.GetRequiredService<IHandler<Message>>();
    
    var userService = scope.ServiceProvider.GetRequiredService<IUser>();

    try
    {
        switch (update)
        {
            case { Message: { } message }:
                _userStates.TryGetValue(message.Chat.Id, out var state);
                if (state == "adding_movie")
                {
                    await userService.AddMovie(message.From.Id, message.Text);
                    await botClient.SendTextMessageAsync(message.Chat.Id, $"Фильм \"{message.Text}\" добавлен!", cancellationToken: cancellationToken);
                    _userStates.Remove(message.Chat.Id);
                }
                else if (state == "my-movies") 
                {
                    await SendUserMovies(botClient, userService, message.Chat.Id, cancellationToken);
                    _userStates.Remove(message.Chat.Id);
                    
                }
                else if (state == "delete_movie")
                {
                    _userStates.Remove(message.Chat.Id);
                }
                else
                {
                    await messageHandler.Handle(message, cancellationToken);
                }
                break;

            case { CallbackQuery: { } query }:
                await HandleCallbackQueryAsync(query, userService, botClient, cancellationToken); // Передаем botClient
                break;

            default:
                _logger.LogWarning("Unhandled update type: {Type}", update.Type);
                break;
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error handling update.");
    }
}

private async Task HandleCallbackQueryAsync(CallbackQuery query, IUser userService, ITelegramBotClient botClient, CancellationToken cancellationToken) // Принимаем botClient
{
    
    
    if (query.Message is not { } message)
    {
        _logger.LogWarning("Callback query message is null.");
        return;
    }

    switch (query.Data)
    {
        case "add-movie":
            _userStates[message.Chat.Id] = "adding_movie";
            await _botClient.SendTextMessageAsync(message.Chat.Id, "Напиши название фильма", cancellationToken: cancellationToken);
            break;
        case "my-movies":
            await SendUserMovies(botClient, userService, message.Chat.Id, cancellationToken);
            await _messageHandler.Handle(query.Message, cancellationToken);
            break;
        case "delete-movie":
            var userMovies = await userService.GetAllMoviesFromUser(message.Chat.Id);
            if (userMovies.Any())
            {
                var inlineKeyboard = new InlineKeyboardMarkup(userMovies.Select(movieName =>
                    new[] { InlineKeyboardButton.WithCallbackData(movieName, $"delete:{movieName}") })); 
                await botClient.SendTextMessageAsync(
                    message.Chat.Id,
                    "Какой фильм удалить?",
                    replyMarkup: inlineKeyboard, 
                    cancellationToken: cancellationToken);
            }
            else
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "У тебя пока нет фильмов для удаления.", cancellationToken: cancellationToken);
            }
            break;

        case string data when data.StartsWith("delete:"): 
            var movieToDelete = data.Substring("delete:".Length);
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var movie = await dbContext.Movies.FirstOrDefaultAsync(m => m.MovieName == movieToDelete);

                if (movie != null)
                {
                    await userService.RemoveMovie(message.Chat.Id, movie.MovieId);
                    await botClient.SendTextMessageAsync(message.Chat.Id, $"Фильм \"{movieToDelete}\" удален.", cancellationToken: cancellationToken);
                    await _messageHandler.Handle(query.Message, cancellationToken);
                }
                
            }
            break;
        default:
            await _botClient.SendTextMessageAsync(message.Chat.Id, "Я не знаю такой команды. Попробуй выбрать что-то из меню.", cancellationToken: cancellationToken);
            break;
    }
    await _botClient.AnswerCallbackQueryAsync(query.Id, cancellationToken: cancellationToken);
}



        private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            if (exception is ApiRequestException apiException)
            {
                _logger.LogError(apiException, "Telegram API error. Code: {ErrorCode}. Message: {Message}",
                    apiException.ErrorCode, apiException.Message);
            }
            else
            {
                _logger.LogError(exception, "Unexpected error occurred.");
            }

            return Task.CompletedTask;
        }
        private async Task SendUserMovies(ITelegramBotClient botClient, IUser userService, long chatId, CancellationToken cancellationToken)
        {
            var userMovies = await userService.GetAllMoviesFromUser(chatId);
            if (userMovies.Any())
            {
                var moviesList = string.Join("\n", userMovies);
                await botClient.SendTextMessageAsync(chatId, $"Твои фильмы:\n{moviesList}", cancellationToken: cancellationToken);
            }
            else
            {
                await botClient.SendTextMessageAsync(chatId, "У тебя пока нет фильмов.", cancellationToken: cancellationToken);
            }
        }
    }
}
