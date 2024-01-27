using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

string token = System.IO.File.ReadAllText("token.txt");

var bot = new TelegramBotClient(token);

using CancellationTokenSource cts = new();

bot.StartReceiving(
    updateHandler: HandleUpdateAsync,
    pollingErrorHandler: HandlePollingErrorAsync,
    receiverOptions: new() { AllowedUpdates = Array.Empty<UpdateType>() },
    cancellationToken: cts.Token
);

var me = await bot.GetMeAsync();

Console.WriteLine($"Start listening for @{me.Username}");

await Task.Delay(Timeout.Infinite);

// Send cancellation request to stop bot
cts.Cancel();

async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
{
    // Only process Message updates: https://core.telegram.org/bots/api#message
    if (update.Message is not { } message)
        return;

    if(message.Photo is PhotoSize[] photoSize) // process photo messages
    {
        if(update.Message.Caption is not { } caption)
            return;
        
        Task task = caption.Trim() switch
        {
            "/qr" => Cmd.QrDecode(bot, update, ct),
            _ => Task.CompletedTask
        };

        await task;

    }
    else if (message.Text is string messageText) // process text messages
    {
        string? cmd = messageText.Trim().Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)[0];

        Task task = cmd switch
        {
            "/github" => Cmd.Github(bot, update, ct),
            "/id" => Cmd.Id(bot, update, ct),
            "/wttr" => Cmd.Wttr(bot, update, ct),
            "/qr" => Cmd.QrEncode(bot, update, ct),
            "/qrwifi" => Cmd.QrEncodeWifi(bot, update, ct),
            "/help" => Cmd.Help(bot, update, ct),
            _ => Task.CompletedTask
        };

        await task;
    }

}

Task HandlePollingErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken cancellationToken)
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
