using Homework09.TelegramBot;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;

// https://telegrambots.github.io/book/1/quickstart.html
var token = System.IO.File.ReadAllText("token.txt");

var botClient = new TelegramBotClient(token);
var fileStorage = new FileStorage();



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
    if (update.Message!.Type == MessageType.Audio || update.Message!.Type == MessageType.Voice 
        || update.Message!.Type == MessageType.Photo || update.Message!.Type == MessageType.Document)
    {
        var fileId = update.Message!.Type switch
        {
            MessageType.Audio => update.Message.Audio!.FileId,
            MessageType.Voice => update.Message.Voice!.FileId,
            MessageType.Photo => update.Message.Photo!.Last().FileId,
            MessageType.Document => update.Message.Document!.FileId
        };
            
        var file = await botClient.GetFileAsync(fileId);
        Console.WriteLine(file.FilePath);
        
        var memoryStream = new MemoryStream();
        await botClient.DownloadFileAsync(file.FilePath, memoryStream);
        fileStorage.SaveFile(file.FilePath, memoryStream.ToArray());
    }

    if (update.Message!.Type == MessageType.Text && update.Message.Text == "/start")
    {
        await botClient.SendTextMessageAsync(
            chatId: update.Message.Chat.Id,
            text: "Отправляйте свои файлы и скачивайте уже отправленные файлы через команду /browse",
            cancellationToken: cancellationToken);
        return;
    }

    if (update.Message!.Type == MessageType.Text && update.Message.Text!.StartsWith("/download "))
    {
        var fileName = update.Message!.Text.Substring("/download ".Length);
        var data = fileStorage.LoadFile(fileName);
        if (data == null)
        {
            await botClient.SendTextMessageAsync(
                chatId: update.Message.Chat.Id,
                text: "Такого файла нет",
                cancellationToken: cancellationToken);
            return;
        }
        

        using (var stream = new MemoryStream(data))
        {
            var inputOnlineFile = new InputOnlineFile(stream, fileName);
            await botClient.SendDocumentAsync(update.Message.Chat.Id, inputOnlineFile);
        }
        return;
    }
    if (update.Message!.Type == MessageType.Text && update.Message.Text == "/browse")
    {
        var files = fileStorage.BrowseFiles();
        var filesText = string.Join("\n", files.Select(file => $"❤︎ {Path.GetFileName(file)} ❤︎")); 
        await botClient.SendTextMessageAsync(
            chatId: update.Message.Chat.Id,
            text: filesText,
            cancellationToken: cancellationToken);

        var replyKeyboardMarkup = new ReplyKeyboardMarkup(
            files.Select(file => new KeyboardButton[] { new KeyboardButton($"/download {Path.GetFileName(file)}") })
           );

       await botClient.SendTextMessageAsync(
            chatId: update.Message.Chat.Id,
            text: "Выберите файл для скачивания",
            replyMarkup: replyKeyboardMarkup,
            cancellationToken: cancellationToken);

        return;
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
        text: "❤︎ Вы сказали ❤︎:\n" + messageText,
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
