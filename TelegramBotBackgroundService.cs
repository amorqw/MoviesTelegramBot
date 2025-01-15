
using System.Reflection.Metadata;
using MoviesTelegramBot.Features;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using MoviesTelegramBot.Interfaces;

namespace MoviesTelegramBot
{
    public class TelegramBotBackgroundService : IHostedService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<TelegramBotBackgroundService> _logger;
        private CancellationTokenSource _cts;

        public TelegramBotBackgroundService(
            ITelegramBotClient botClient,
            IServiceScopeFactory scopeFactory,
            ILogger<TelegramBotBackgroundService> logger)
        {
            _botClient = botClient;
            _scopeFactory = scopeFactory;
            _logger = logger;
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


private readonly Dictionary<long, string> _userStates = new(); // Словарь для отслеживания состояния пользователя (например, "добавляет фильм")

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
                // Проверяем, находится ли пользователь в состоянии "добавления фильма"
                if (_userStates.TryGetValue(message.Chat.Id, out var state) && state == "adding_movie")
                {
                    await userService.AddMovie(message.From.Id, message.Text); // Добавляем фильм в базу данных
                    await botClient.SendTextMessageAsync(message.Chat.Id, $"Фильм \"{message.Text}\" добавлен!", cancellationToken: cancellationToken); // Подтверждаем добавление фильма пользователю
                    _userStates.Remove(message.Chat.Id); // Удаляем состояние пользователя после завершения действия
                }
                else
                {
                    await messageHandler.Handle(message,cancellationToken);
                }
                break;

            case { CallbackQuery: { } query }:
                await HandleCallbackQueryAsync(query, userService, cancellationToken); // Обрабатываем нажатие кнопок
                break;

            default:
                _logger.LogWarning("Unhandled update type: {Type}", update.Type); // Логируем необработанные типы обновлений
                break;
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error handling update."); // Логируем ошибки
    }
}

private async Task HandleCallbackQueryAsync(CallbackQuery query, IUser userService, CancellationToken cancellationToken)
{
    if (query.Message is not { } message)
    {
        _logger.LogWarning("Callback query message is null."); // Предупреждаем, если сообщение из запроса пустое
        return;
    }

    switch (query.Data)
    {
        case "add-movie":
            _userStates[message.Chat.Id] = "adding_movie"; // Устанавливаем состояние "добавления фильма" для текущего пользователя
            await _botClient.SendTextMessageAsync(
                message.Chat.Id,
                "Напиши название фильма", // Информируем пользователя о следующем шаге
                cancellationToken: cancellationToken);
            break;

        default:
            // Если команда не распознана, отправляем стандартное сообщение
            await _botClient.SendTextMessageAsync(
                message.Chat.Id,
                "Я не знаю такой команды. Попробуй выбрать что-то из меню.",
                cancellationToken: cancellationToken);
            break;
    }
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
    }
}
