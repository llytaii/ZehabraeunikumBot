using System.Linq;

using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

using QRCoder;
using System.Text.RegularExpressions;
using System.ComponentModel;
using ZXing;
using SkiaSharp;

public static class Cmd
{
    // Image based commands
    public static async Task QrDecode(ITelegramBotClient bot, Update update, CancellationToken ct)
    {
        var fileId = update.Message!.Photo!.Last().FileId;
        var fileInfo = await bot.GetFileAsync(fileId);
        var filePath = fileInfo.FilePath;

        // Create a MemoryStream to hold the downloaded data
        await using MemoryStream memoryStream = new MemoryStream();

        // Download the file into the memory stream
        await bot.DownloadFileAsync(
            filePath: filePath!,
            destination: memoryStream,
            cancellationToken: ct
        );

        var skBitmap = SKBitmap.Decode(memoryStream.ToArray());

        var barcodeReader = new BarcodeReaderGeneric();
        var result = barcodeReader.Decode(skBitmap);

        await bot.SendTextMessageAsync(
            chatId: update.Message!.Chat.Id,
            text: result != null ? result.Text : "Decoding the barcode failed, maybe it isnt correct.",
            cancellationToken: ct
        );
    }

    // Text based commands
    public static async Task Id(ITelegramBotClient bot, Update update, CancellationToken ct)
    {
        await bot.SendTextMessageAsync(
            chatId: update.Message!.Chat.Id,
            text: update.Message.Chat.Id.ToString(),
            cancellationToken: ct
        );
    }
    public static async Task QrEncodeWifi(ITelegramBotClient bot, Update update, CancellationToken ct)
    {
        string messageText = update.Message!.Text!;

        string pattern = ".*?\"([^\"]*)\"\\s*\"([^\"]*)\"";

        Match match = Regex.Match(messageText, pattern);

        if (!match.Success)
        {
            await bot.SendTextMessageAsync(
                chatId: update.Message.Chat.Id,
                text: "Wrong format, be sure to write: /qrwifi \"<SSID>\" \"<PASSWORD>\"",
                cancellationToken: ct
            );
            return;
        }

        string ssid = match.Groups[1].Value;
        string password = match.Groups[2].Value;

        var qrGenerator = new QRCodeGenerator();
        var wifiPayload = new PayloadGenerator.WiFi(ssid, password, PayloadGenerator.WiFi.Authentication.WPA);
        var qrCodeData = qrGenerator.CreateQrCode(wifiPayload.ToString(), QRCodeGenerator.ECCLevel.H);
        var qrCode = new PngByteQRCode(qrCodeData);
        var png = qrCode.GetGraphic(20);

        await bot.SendPhotoAsync(
            chatId: update.Message.Chat.Id,
            photo: InputFile.FromStream(new MemoryStream(png)),
            caption: $"QR-Code for Wifi: {ssid}",
            parseMode: ParseMode.Html,
            cancellationToken: ct
        );
    }
    public static async Task QrEncode(ITelegramBotClient bot, Update update, CancellationToken ct)
    {
        string messageText = update.Message!.Text!;

        int idx = messageText.IndexOf(' ');
        if (idx == -1) return;

        string text = messageText.Substring(idx).Trim();
        if (string.IsNullOrEmpty(text)) return;

        var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.H);
        var qrCode = new PngByteQRCode(qrCodeData);
        var png = qrCode.GetGraphic(20);

        await bot.SendPhotoAsync(
            chatId: update.Message.Chat.Id,
            photo: InputFile.FromStream(new MemoryStream(png)),
            caption: text,
            parseMode: ParseMode.Html,
            cancellationToken: ct
        );
    }
    public static async Task Wttr(ITelegramBotClient bot, Update update, CancellationToken ct)
    {
        List<string> locations = update.Message!.Text!
                            .Trim()
                            .Split(' ',
                                    StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries
                            )
                            .Skip(1) // skip command
                            .ToList();

        if (locations.Count == 0) locations.Add("weilheim");

        using (var client = new HttpClient())
        {
            foreach (var location in locations)
            {
                var response = await client.GetAsync($"http://v2.wttr.in/{location}.png");

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        byte[] png = await response.Content.ReadAsByteArrayAsync();
                        await bot.SendPhotoAsync(
                            chatId: update.Message.Chat.Id,
                            photo: InputFile.FromStream(new MemoryStream(png)),
                            caption: $"Weather in {location}",
                            parseMode: ParseMode.Html,
                            cancellationToken: ct
                        );

                    }
                    catch (Exception ex)
                    {
                        await bot.SendTextMessageAsync(
                            chatId: update.Message!.Chat.Id,
                            text: $"Error while fetching wttr.in for png: {ex}",
                            cancellationToken: ct
                        );

                    }
                }
                else
                {
                    await bot.SendTextMessageAsync(
                        chatId: update.Message!.Chat.Id,
                        text: $"Error while fetching wttr.in for png",
                        cancellationToken: ct
                    );
                }
            }

        }
    }
    public static async Task Help(ITelegramBotClient bot, Update update, CancellationToken ct)
    {
        string help = "";
        help += "/qr <text> : encodes <text> into qr code\n";
        help += "/qr as barcode image capture text decodes the image\n";
        help += "/qrwifi <ssid> <password> : encodes wifi data into qr code (wpa only)\n";
        help += "/wttr (location1) (location2) : tries to fetch wttr.in for weather png for each location. weilheim is the default location if none is given.\n";
        help += "/help : this help info\n";
        help += "/id : gets the telegram id of the active chat (private chat or group chat)";
        await bot.SendTextMessageAsync(
            chatId: update.Message!.Chat.Id,
            text: help,
            cancellationToken: ct
        );
    }
}

