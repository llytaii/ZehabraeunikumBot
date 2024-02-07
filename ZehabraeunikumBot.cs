
using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Weather.NET;

public class ZehabraeunikumBot(string token)
{
    private TelegramBotClient Bot = new(token);
    public delegate Task BotCommand(ITelegramBotClient bot, Update update, CancellationToken ct);
    private Dictionary<string, BotCommand> MessageCommandMap = new();
    private Dictionary<string, BotCommand> PhotoCommandMap = new();

    public void AddMessageCommand(string command, BotCommand function)
    {
        this.MessageCommandMap.Add(command, function);
    }
    public void AddPhotoCommand(string command, BotCommand function)
    {
        this.PhotoCommandMap.Add(command, function);
    }

    public async Task Run()
    {
        var me = await this.Bot.GetMeAsync();

        Console.WriteLine($"Start listening for @{me.Username}");
        Bot.StartReceiving(
            updateHandler: this.HandleUpdateAsync,
            pollingErrorHandler: this.HandlePollingErrorAsync,
            receiverOptions: new() { AllowedUpdates = Array.Empty<UpdateType>() },
            cancellationToken: CancellationToken.None
        );
        await Task.Delay(Timeout.Infinite);
    }


    private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
    {
        // Only process Message updates: https://core.telegram.org/bots/api#message
        if (update.Message is not { } message)
            return;

        Task task = Task.CompletedTask;

        if (message.Photo is PhotoSize[] photoSize) // process photo messages
        {
            if (update.Message.Caption is not { } caption)
                return;

            task = this.PhotoCommandMap.TryGetValue(caption.Trim(), out var command) 
                    ? command(bot, update, ct) 
                    : Task.CompletedTask;
        }
        else if (message.Text is string messageText) // process text messages
        {
            string cmd = messageText.Trim().Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)[0] ?? "";

            task = this.MessageCommandMap.TryGetValue(cmd, out var command) 
                    ? command(bot, update, ct) 
                    : Task.CompletedTask;
        }

        await task;
    }

    private Task HandlePollingErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken cancellationToken)
    {
        var ErrorMessage = exception switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        Console.WriteLine(ErrorMessage);
        return Task.CompletedTask;
    }

}