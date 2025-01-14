using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MoviesTelegramBot.Features;
using MoviesTelegramBot.Options;
using MoviesTelegramBot;
using MoviesTelegramBot.Data;
using MoviesTelegramBot.Interfaces;
using MoviesTelegramBot.Services;
using Telegram.Bot;
using Telegram.Bot.Types;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddScoped<IUser,UserService>();

builder.Services.AddTransient<TelegramBotBackgroundService>();

builder.Services.AddTransient<ITelegramBotClient, TelegramBotClient>(serviceProvider =>
{
    var token = serviceProvider.GetRequiredService<IOptions<TelegramOptions>>().Value.Token;

    return new(token);
});

builder.Services.AddTransient<IHandler<Message>, MessageHandler>();
builder.Services.Configure<TelegramOptions>(builder.Configuration.GetSection(TelegramOptions.Telegram));
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("ConnectionStrings")));
var host = builder.Build();
host.Run();