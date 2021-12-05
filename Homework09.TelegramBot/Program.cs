using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

var token = System.IO.File.ReadAllText("token.txt");

var botClient = new TelegramBotClient(token);




using var cts = new CancellationTokenSource();

// StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
var receiverOptions = new ReceiverOptions
{
    AllowedUpdates = { } // receive all update types
};
botClient.StartReceiving(
    HandleUpdateAsync,
    HandleErrorAsync,
    receiverOptions,
    cancellationToken: cts.Token);

var me = await botClient.GetMeAsync();
Console.WriteLine($"Hello, World! I am user {me.Id} and my name is {me.FirstName}.");

Console.WriteLine($"Start listening for @{me.Username}");
Console.ReadLine();

// Send cancellation request to stop bot
cts.Cancel();

async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    // Only process Message updates: https://core.telegram.org/bots/api#message
    if (update.Type != UpdateType.Message)
        return;
    Console.WriteLine(update.Message!.Type);
    if (update.Message!.Type == MessageType.Audio)
    {
        var file = await botClient.GetFileAsync(update.Message.Audio!.FileId);
        Console.WriteLine(file.FilePath);
    }
    if (update.Message!.Type == MessageType.Voice)
    {
        var file = await botClient.GetFileAsync(update.Message.Voice!.FileId);
        Console.WriteLine(file.FilePath);
        using var localFile = System.IO.File.Create(Path.GetFileName(file.FilePath));
        await botClient.DownloadFileAsync(file.FilePath, localFile);
    }
  
    // Only process text messages
    if (update.Message!.Type != MessageType.Text)
        return;
    //update.Message.Audio
    

    var chatId = update.Message.Chat.Id;
    var messageText = update.Message.Text;

    Console.WriteLine($"Received a '{messageText}' message in chat {chatId}.");

    // Echo received message text
    Message sentMessage = await botClient.SendTextMessageAsync(
        chatId: chatId,
        text: "❤︎ You said ❤︎:\n" + messageText,
        cancellationToken: cancellationToken);
}

Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
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







//Что нужно сделать
//Создайте бота для одной из следующих платформ:

//Twitch,
//Discord,
//Telegram.


//Бот обладает следующим набором функций:

//Принимает сообщения и команды от пользователя.
//Сохраняет аудиосообщения, картинки и произвольные файлы.
//Позволяет пользователю просмотреть список загруженных файлов.
//Позволяет скачать выбранный файл.


//Команды можно делать разные, но среди них должна присутствовать команда /start.



//Вы можете сделать бота на любую тематику. Например, ваш бот может искать видео на YouTube, выводить курс криптовалют, отображать данные о погоде и так далее.



//Что оценивается
//Бот принимает текстовые сообщения.
//Бот реагирует на команду /start.
//Бот позволяет сохранять на диск изображения, аудио- и другие файлы.
//С помощью произвольной команды можно просмотреть список сохранённых файлов и скачать любой из них.
