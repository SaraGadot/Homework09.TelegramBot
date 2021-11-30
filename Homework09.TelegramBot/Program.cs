using Telegram.Bot;

var token = "";

var botClient = new TelegramBotClient(token);

var me = await botClient.GetMeAsync();
Console.WriteLine($"Hello, World! I am user {me.Id} and my name is {me.FirstName}.");