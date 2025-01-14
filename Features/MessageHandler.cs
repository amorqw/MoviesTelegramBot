using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace MoviesTelegramBot.Features;

public class MessageHandler : IHandler<Message>
{
    private readonly ITelegramBotClient _botClient;

    public MessageHandler(ITelegramBotClient botClient)
    {
        _botClient = botClient;
    }

    public async Task Handle(Message message, CancellationToken cancellationToken)
    {
        InlineKeyboardMarkup inlineKeyboard = new(
        [
            [InlineKeyboardButton.WithCallbackData("Мой список", "my-movies")],
            [InlineKeyboardButton.WithCallbackData("Добавить фильм", "add-movie"),
            InlineKeyboardButton.WithCallbackData("Удалить фильм из списка", "delete-movie")],
        ]);

        await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "Здесь вы можете посмотреть фильмы, добавленные в список желаемого, или добавить новые ",
            replyMarkup: inlineKeyboard,
            cancellationToken: cancellationToken);
    }
}