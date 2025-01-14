using System.Text.Json;
using Microsoft.VisualBasic;
using MoviesTelegramBot.Features;
using MoviesTelegramBot.Interfaces;
using MoviesTelegramBot.Services;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace MoviesTelegramBot;

public class TelegramBotBackgroundService : BackgroundService
{
    private readonly ITelegramBotClient _botClient;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TelegramBotBackgroundService> _logger;
    private readonly IUser _userService;

    public TelegramBotBackgroundService(
        ITelegramBotClient botClient,
        IServiceScopeFactory scopeFactory,
        ILogger<TelegramBotBackgroundService> logger,
        IUser userService)
    {
        _botClient = botClient;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _userService = userService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ReceiverOptions receiverOptions = new()
        {
            AllowedUpdates = new[] { Telegram.Bot.Types.Enums.UpdateType.Message, Telegram.Bot.Types.Enums.UpdateType.CallbackQuery }
        };

        while (!stoppingToken.IsCancellationRequested)
        {
            await _botClient.ReceiveAsync(
                updateHandler: HandleUpdateAsync,
                errorHandler: HandleErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: stoppingToken);
        }
    }

    private async Task HandleUpdateAsync(
        ITelegramBotClient botClient,
        Update update,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Received update: {JsonSerializer.Serialize(update)}");
        var scope = _scopeFactory.CreateScope();

        var messageHandler = scope.ServiceProvider.GetRequiredService<IHandler<Message>>();

        var handler = update switch
        {
            { Message: { } message } => messageHandler.Handle(message, cancellationToken),
            { CallbackQuery: { } query } => CallbackQueryHandler(query, cancellationToken),
            _ => UnknownUpdateHandlerAsync(update, cancellationToken)
        };

        await handler;
    }

    private Task UnknownUpdateHandlerAsync(Update update, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Unknown type message");
        return Task.CompletedTask;
    }

    private async Task CallbackQueryHandler(CallbackQuery query, CancellationToken cancellationToken)
    {
        if (query.Message is not { } message)
            return;

        switch (query.Data)
        {
            case "lessons-info":
                await _botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "Вот тебе информация о занятиях, дорогой друг",
                    cancellationToken: cancellationToken);
                return;
            case "my-movies":
                await _botClient.SendMessage(
                    message.Chat.Id,
                    "Твой список фильмов"
                    //to do подкючить бд и выводить из бд список фильмов 
                    
                );
                return;
            case "add-movie":
                await _botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text:"Напиши название фильма"
                );
                await _userService.AddMovie(message.From.Id, message.Text);
                return;
            
            case "delete-movie":
                await _botClient.SendTextMessageAsync(
                    message.Chat.Id,
                    "Выбери фильм который хочешь удалить"
                    // TO do выводить список фильмов как кнопки -> при нажатии выбор либо удалить либо изменить название
                );
                return;
        }
    }

    private Task HandleErrorAsync(
        ITelegramBotClient botClient,
        Exception exception,
        CancellationToken cancellationToken)
    {
        switch (exception)
        {
            case ApiRequestException apiRequestException:
                _logger.LogError(
                    apiRequestException,
                    "Telegram API Error:\n[{errorCode}]\n{message}",
                    apiRequestException.ErrorCode,
                    apiRequestException.Message);
                return Task.CompletedTask;

            default:
                _logger.LogError(exception, "Error while processing message in telegram bot");
                return Task.CompletedTask;
        }
    }
}