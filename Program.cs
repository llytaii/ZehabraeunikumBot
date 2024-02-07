using Microsoft.Extensions.Configuration;

var settings = new ConfigurationBuilder().AddJsonFile("settings.json", false).Build();
var zhbBot = new ZehabraeunikumBot(settings);

await zhbBot.Run();


