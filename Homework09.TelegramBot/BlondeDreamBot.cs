using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;

namespace Homework09.TelegramBot;

internal class BlondeDreamBot
{
    TelegramBotClient botClient;
    FileStorage fileStorage = new FileStorage();

    public BlondeDreamBot(string token)
    {
        botClient = new TelegramBotClient(token);
    }
    public async Task Execute(CancellationToken cancellationToken)
    {
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = { } // receive all update types
        };
        botClient.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions,
            cancellationToken: cancellationToken);

        var me = await botClient.GetMeAsync();
        Console.WriteLine($"Hello, World! I am user {me.Id} and my name is {me.FirstName}.");

        Console.WriteLine($"Start listening for @{me.Username}");

    }

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

}
