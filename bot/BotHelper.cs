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
        private readonly Dictionary<long, string> SelectedUserService = new Dictionary<long, string>();
        string errorReport = "";
        string markWorkSoft = "";
        string describeService = "";
        private long adminChatId = 1895025;

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

        async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken) // обработка состояний и кнопок
        {
            try
            {
                if (update.Type == Telegram.Bot.Types.Enums.UpdateType.CallbackQuery)
                {
                    await HandleCallBackQuery(update.CallbackQuery);
                    return;
                }

                var message = update.Message;

                // Проверяем, что сообщение не null и его тип - текст
                if (message != null && message.Type == Telegram.Bot.Types.Enums.MessageType.Text)
                {
                    var MessageText = message.Text;
                    Console.WriteLine($"{message.Chat.Id} {message.Chat.FirstName ?? null}\t {message.Text}");

                    // Обработка команд
                    if (MessageText.StartsWith("/"))
                    {
                        await CommandHandle(message);
                        return; // Выходим, чтобы не обрабатывать текстовые сообщения
                    }

                    else if (!MessageText.StartsWith("/")) //обработка кнопок___________
                    {
                        if (MessageText == "Сообщить об ошибке")
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

                        else if (message.Text.ToLower() == "оценить работу сайта и бота")
                        {
                            var inlineKeyBoard = new InlineKeyboardMarkup(new[]
                            {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Работа бота", "mark_bot"),
                            InlineKeyboardButton.WithCallbackData("Работа сайта", "mark_site")
                        }
                        });
                            await botClient.SendMessage(message.Chat.Id, "Выберите что вы хотите оценить: ", replyMarkup: inlineKeyBoard);
                        }

                        switch (message.Text)
                        {
                            case "Показать мой ID":
                                await botClient.SendMessage(message.Chat.Id, $"{message.Chat.Id}");
                                break;
                        }
                    }

                    // Проверяем состояние пользователя
                    if (userStates.TryGetValue(message.Chat.Id, out var state)) //_________________обработка состояний____________________________________
                    {
                        switch (state)
                        {
                            case "awaiting_error_description":
                                errorReport += MessageText + "; id пользователя: " + message.Chat.Id;
                                await botClient.SendMessage(message.Chat.Id, "Спасибо за ответ, проблема взята в обработку");
                                userStates.Remove(message.Chat.Id);
                                await botClient.SendMessage(adminChatId, $"{errorReport}");
                                errorReport = "";
                                break;

                            case "awaiting_mark_work":
                                markWorkSoft += MessageText + "; Id пользователя: " + message.Chat.Id;
                                await botClient.SendMessage(message.Chat.Id, "Спасибо за оценку нашей работы ");
                                userStates.Remove(message.Chat.Id);
                                await botClient.SendMessage(adminChatId, $"{markWorkSoft}");
                                markWorkSoft = "";
                                break;



                            case "awaiting_serviceDescribtion":
                                if (SelectedUserService.TryGetValue(message.Chat.Id, out var userOrder))
                                {
                                    userOrder += MessageText; // Добавляем описание к заказу
                                    SelectedUserService[message.Chat.Id] = userOrder; // Сохраняем обновленное значение
                                    await botClient.SendMessage(message.Chat.Id, "Спасибо за заказ, он отправлен в статус разработки");
                                    await botClient.SendMessage(adminChatId, $"{userOrder}; id пользователя: {message.Chat.Id}"); // Отправляем заказ
                                }
                                userStates.Remove(message.Chat.Id); // Удаляем состояние после обработки
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private async Task CommandHandle(Message message) //обработка команд
        {
            var MessageText = message.Text;

            switch (MessageText)
            {
                case "/start":
                    await SetStartOptions(message);
                    break;


                case "/service":
                    var inlineButton = new InlineKeyboardMarkup(new[] {
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("Сайт", "service_createSite"),
                                InlineKeyboardButton.WithCallbackData("Бот", "service_createBot"),
                                InlineKeyboardButton.WithCallbackData("Сотрудничество", "service_partners"),
                                InlineKeyboardButton.WithCallbackData("Другая услуга", "service_otherService")
                            }
                            });
                    await botClient.SendMessage(message.Chat.Id, "📋 *Наши услуги:*\n\n" + "1️⃣ *Разработка сайта*\n" + "2️⃣ *Разработка телеграм-бота*\n" +
                    "3️⃣ *Предложить сотрудничество*\n" +
                    "4️⃣ *Заказать услугу не из перечня*\n\n" +
                    "Пожалуйста, выберите номер услуги или напишите свой запрос.",
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: inlineButton);
                    break;

                case "/report":
                    var inlineKeyBoard = new InlineKeyboardMarkup(new[]
                           {
                        new[]
                        {
                             InlineKeyboardButton.WithCallbackData("Ошибка на сайте", "error_site"),
                             InlineKeyboardButton.WithCallbackData("Ошибка в боте", "error_bot")
                        }
                    });
                    await botClient.SendMessage(message.Chat.Id, "Выберите тип ошибки", replyMarkup: inlineKeyBoard);
                    break;

                case "/rate":

                    var inlineKeyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Работа бота", "mark_bot"),
                            InlineKeyboardButton.WithCallbackData("Работа сайта", "mark_site")
                        }
                        });
                    await botClient.SendMessage(message.Chat.Id, "Выберите что вы хотите оценить: ", replyMarkup: inlineKeyboard);
                    break;

                case "/agree":
                    await botClient.SendMessage(message.Chat.Id, "Спасибо за согласие! В дальнейшем ваши контакты будут доступны только программистам.");
                    await botClient.SendMessage(message.Chat.Id, "Опишите ваш запрос.");
                    userStates[message.Chat.Id] = "awaiting_serviceDescribtion";
                    break;

                case "/cancel":
                    await botClient.SendMessage(message.Chat.Id, "Ваш запрос отменён!");
                    userStates.Remove(message.Chat.Id);
                    SelectedUserService.Remove(message.Chat.Id);
                    break;

                case "/assistant":
                    await botClient.SendMessage(message.Chat.Id, "Вы запросили связь с оператором, он уже получил уведомление и в скором времени свяжется с вами");
                    await botClient.SendMessage(adminChatId, "Запрос на связь с оператором");
                    Console.WriteLine($"Запрос на связь с оператором от пользователя {message.Chat.Id}");
                    Console.Beep(1000,100);
                    break;
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
            var button4 = new KeyboardButton("Оценить работу сайта и бота");


            replyMarkup.Keyboard = new[]
            {
            new[] { button1, button2},
            new[] { button3, button4}
            };

            await botClient.SendMessage(message.Chat.Id, $"{message.Chat.FirstName}, Выберите опцию:", replyMarkup: replyMarkup);
            return;
        }

        private async Task HandleCallBackQuery(CallbackQuery callbackQuery) // Для inlineButton
        {

            switch (callbackQuery.Data)
            {
                case "error_bot": //обработка ошибок бота
                    await botClient.SendMessage(callbackQuery.Message.Chat.Id, "Опишите проблему: ");
                    userStates[callbackQuery.From.Id] = "awaiting_error_description";
                    errorReport += "Бот: ";
                    break;

                case "error_site": // обработка ошибок сайта
                    await botClient.SendMessage(callbackQuery.Message.Chat.Id, "Опишите проблему: ");
                    userStates[callbackQuery.From.Id] = "awaiting_error_description";
                    errorReport += "Сайт: ";
                    break;

                case "mark_bot": // регистрация оценки бота
                    await botClient.SendMessage(callbackQuery.Message.Chat.Id, "Поставьте свою оценку от 1 до 10, а также можете написать комментарий: ");
                    userStates[callbackQuery.From.Id] = "awaiting_mark_work";
                    markWorkSoft += "Оценка бота: ";
                    break;

                case "mark_site": // регистрация оценки сайта
                    await botClient.SendMessage(callbackQuery.Message.Chat.Id, "Поставьте свою оценку от 1 до 10, а также можете написать комментарий: ");
                    userStates[callbackQuery.From.Id] = "awaiting_mark_work";
                    markWorkSoft += "Оценка сайта: ";
                    break;

                // _______________________________________обработка заказов_______________________________

                case "service_createSite":
                    await botClient.SendMessage(callbackQuery.Message.Chat.Id, "Перед тем как продолжить, пожалуйста, подтвердите согласие на обработку ваших персональных данных. " +
        "Отправьте /agree для подтверждения или /cancel для отмены.");
                    SelectedUserService[callbackQuery.From.Id] = "order_createSite";
                    userStates[callbackQuery.From.Id] = "awaiting_consent";
                    describeService += callbackQuery.Message.Text;
                    break;

                case "service_createBot":
                    await botClient.SendMessage(callbackQuery.Message.Chat.Id, "Перед тем как продолжить, пожалуйста, подтвердите согласие на обработку ваших персональных данных. " +
        "Отправьте /agree для подтверждения или /cancel для отмены.");
                    SelectedUserService[callbackQuery.From.Id] = "order_createBot";
                    userStates[callbackQuery.From.Id] = "awaiting_consent";
                    describeService += callbackQuery.Message.Text;
                    break;

                case "service_partners":
                    await botClient.SendMessage(callbackQuery.Message.Chat.Id, "Перед тем как продолжить, пожалуйста, подтвердите согласие на обработку ваших персональных данных. "
                        + "Отправьте /agree для подтверждения или /cancel для отмены.");
                    SelectedUserService[callbackQuery.From.Id] = "order_partners";
                    userStates[callbackQuery.From.Id] = "awaiting_consent";
                    describeService += callbackQuery.Message.Text;
                    break;
                case "service_otherService":
                    await botClient.SendMessage(callbackQuery.Message.Chat.Id, "Перед тем как продолжить, пожалуйста, подтвердите согласие на обработку ваших персональных данных. " +
        "Отправьте /agree для подтверждения или /cancel для отмены.");
                    SelectedUserService[callbackQuery.From.Id] = "order_otherService";
                    userStates[callbackQuery.From.Id] = "awaiting_consent";
                    describeService += callbackQuery.Message.Text;
                    break;
            }
        }
    }
}