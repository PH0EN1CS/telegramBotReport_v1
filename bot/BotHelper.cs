using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace bot
{
    internal class BotHelper
    {
        private string token;
        private readonly CancellationTokenSource cts = new CancellationTokenSource();
        static TelegramBotClient botClient;
        private readonly Dictionary<long, string> userStates = new Dictionary<long, string>();
        string errorReport = "";

        public BotHelper(string token)
        {
            this.token = token;
            botClient = new TelegramBotClient(token);

            var _receiverOptions = new ReceiverOptions // получение обновлений от пользователя чата
            {
                AllowedUpdates = { } // ограничения получемых(обрабатываемых) сообщений
            };
            try
            {
                botClient.StartReceiving(HandleUpdateAsync, HandleErrorAsync, _receiverOptions, cancellationToken: cts.Token);
                var infoAboutBot = botClient.GetMe().Result;
                Console.WriteLine($"Бот {infoAboutBot} запущен без ошибок");

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            Console.ReadLine();
        }

        async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                if (update.Type == Telegram.Bot.Types.Enums.UpdateType.CallbackQuery)
                {
                    await HandleCallBackQuery(update.CallbackQuery);
                    return;
                }

                var message = update.Message;
                if (message != null || message.Type == Telegram.Bot.Types.Enums.MessageType.Text)
                {
                    Console.WriteLine($"{message.Chat.Id} {message.Chat.FirstName ?? null}\t {message.Text}");

                    if (userStates.TryGetValue(message.Chat.Id, out var state))
                    {
                        if (state == "awaiting_error_description")
                        {
                            errorReport += message.Text + "id пользователя: " + message.Chat.Id;
                            await botClient.SendMessage(message.Chat.Id, "Спасибо за ответ, проблема взята в обработку");
                            userStates.Remove(message.Chat.Id);
                            // Здесь можно сохранить или обработать errorReport
                           // await botClient.SendMessage(admin id, $"{errorReport}");
                            return;
                        }
                    }

                    var MessageText = message.Text;

                    if (MessageText.ToLower() == "/start")
                    {
                        await SetStartOptions(message);
                    }
                    else if (MessageText == "Сообщить об ошибке")
                    {
                        var inlineKeyBoard = new InlineKeyboardMarkup(new[]
                        {
                        new[]
                        {
                             InlineKeyboardButton.WithCallbackData("Ошибка на сайте", "error_site"),
                             InlineKeyboardButton.WithCallbackData("Ошибка в боте", "error_bot")
                        }
                    });
                        await botClient.SendMessage(message.Chat.Id, "Выберите тип ошибки", replyMarkup: inlineKeyBoard);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }


        async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Telegram Error:\t [{apiRequestException.ErrorCode}]\t {apiRequestException.Message}",
                _ => exception.ToString()
            };
            Console.WriteLine(ErrorMessage);
            return;
        }


        private async Task SetStartOptions(Message message)
        {
            var replyMarkup = new ReplyKeyboardMarkup
            {
                ResizeKeyboard = true, // автоматический размер под экран пользователя
                OneTimeKeyboard = true // исчезнет после нажатия, что бы не занимать внимание пользователя
            };

            var button1 = new KeyboardButton("Сообщить об ошибке");
            var button2 = new KeyboardButton("Показать мой ID");
            var button3 = new KeyboardButton("Официальный сайт");


            replyMarkup.Keyboard = new[]
            {
            new[] { button1, button2},
            new[] { button3}
            };

            await botClient.SendMessage(message.Chat.Id, $"{message.Chat.FirstName}, Выберите опцию:", replyMarkup: replyMarkup);
            return;
        }

        private async Task HandleCallBackQuery(CallbackQuery callbackQuery) // Для inlineButton
        {

            switch (callbackQuery.Data)
            {
                case "error_bot":
                    await botClient.SendMessage(callbackQuery.Message.Chat.Id, "Опишите проблему: ");
                    userStates[callbackQuery.From.Id] = "awaiting_error_description";
                    errorReport += "Бот: ";
                    break;

                case "error_site":
                    await botClient.SendMessage(callbackQuery.Message.Chat.Id, "Опишите проблему: ");
                    userStates[callbackQuery.From.Id] = "awaiting_error_description";
                    errorReport += "Сайт: ";
                    break;
            }
        }
    }
}