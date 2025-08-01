using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramStore.Data;
using TelegramStore.Data.Models;

namespace TelegramGameTest.TelegramAPI
{
    public class TelegramAPI
    {
        private ITelegramBotClient _botClient;

        private DataBase _dataBase;

        private Dictionary<long, Action<Update>> _onResponse;

        private IEnumerable<string> _admins;

        private TelegramAdminAPI _telegramAdminAPI;

        public TelegramAPI(string token, DataBase db, IEnumerable<string> admins)
        {
            _botClient = new TelegramBotClient(token);
            _dataBase = db;
            _onResponse = new Dictionary<long, Action<Update>>();
            _telegramAdminAPI = new TelegramAdminAPI(_botClient, db, _onResponse);
            _admins = admins;
        }


        public void Start()
        {
            _botClient.StartReceiving(new DefaultUpdateHandler(HandleUpdateAsync, HandleErrorAsync));
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                if (update.Type == UpdateType.Message)
                {
                    var message = update.Message;
                    if (_onResponse.TryGetValue(message.From.Id, out var action))
                    {
                        _onResponse[message.From.Id] = null;
                        action?.Invoke(update);
                    }
                    if (_admins.Contains(message.From.Username))
                    {
                        await _telegramAdminAPI.HandleUpdateAsync(botClient, update, cancellationToken);
                    }
                    if (message.Text.StartsWith("/start"))
                    {
                        await SendReplyKeyboard(_botClient, message.Chat.Id);
                        return;
                    }
                    if (message.Text == "Забронировать товар")
                    {
                        await SelectProductToAdd(_botClient, message);
                        return;
                    }
                    if (message.Text == "Мои брони")
                    {
                        await MyOrders(_botClient, message);
                        return;
                    }
                    if (message.Text == "Отменить бронь")
                    {
                        await SelectProductToCancel(_botClient, message);
                        return;
                    }
                    return;
                }
                if (update.Type == UpdateType.CallbackQuery)
                {
                    var query = update.CallbackQuery;

                    if (_onResponse.TryGetValue(query.From.Id, out var action))
                    {
                        _onResponse[query.From.Id] = null;
                        action?.Invoke(update);
                    }
                    if (_admins.Contains(query.From.Username))
                    {
                        await _telegramAdminAPI.HandleUpdateAsync(botClient, update, cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public async Task AddOrder(Update update, Chat chat, Product product)
        {
            _onResponse[update.CallbackQuery.From.Id] = async (update) => {
                var message = update.Message;
                if (uint.TryParse(message?.Text, out uint val) && val > 0)
                {
                    if (await _dataBase.AddOrder(product.Id, $"{message.From.FirstName} {message.From.LastName}", message.From.Username, (int)val))
                    {
                        await _botClient.SendMessage(
                            chatId: chat,
                            text: $"Забронировано {product.Name} - {val}шт"
                        );

                        foreach(var (admin, chatId) in await _dataBase.GetAdmins())
                        {
                            _botClient.SendMessage(
                                chatId: chatId,
                                text: $"{message.From.FirstName} {message.From.LastName} ({message.From.Username}) забронировал {product.Name} - {val}шт"
                            );
                        }
                    }
                    else
                    {
                        await _botClient.SendMessage(
                            chatId: chat,
                            text: $"На складе не достаточно товара"
                        );
                    }
                }
                else
                {
                    await _botClient.SendMessage(
                        chatId: chat,
                        text: $"Неверная запись"
                    );
                }
            };
            await _botClient.SendMessage(
                chatId: chat,
                text: $"Введите количество {product.Name}:"
            );
            await _botClient.AnswerCallbackQuery(update.CallbackQuery.Id, text: "Введите количество:");
        }

        // Send a message with reply keyboard buttons
        private async Task SendReplyKeyboard(ITelegramBotClient bot, long chatId)
        {
             var replyKeyboard = new ReplyKeyboardMarkup(new[]
                {
                    new KeyboardButton[] { "Забронировать товар"},
                    new KeyboardButton[] { "Мои брони"},
                    new KeyboardButton[] { "Отменить бронь" },
                }
            )
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = true
            };

            await bot.SendMessage(
                chatId: chatId,
                text: "Ваше действие:",
                replyMarkup: replyKeyboard
            );
        }

        private async Task SelectProductToAdd(ITelegramBotClient bot, Message message)
        {
            var products = (await _dataBase.GetProducts()).ToList();
            var inlineKeyboard = new InlineKeyboardMarkup(
                products.Select(product => 
                    new[] { InlineKeyboardButton.WithCallbackData($"{product.Name} ({product.Count}шт)", $"AddOrderProduct_{product.Id}") })
                
            );

            var sentMessage = await bot.SendMessage(
                chatId: message.Chat,
                text: products.Count > 0? "Выберите товар": "На складе нет товаров",
                replyMarkup: inlineKeyboard
            );
            _onResponse[message.From.Id] = (update) => {

                if (update.Type == UpdateType.CallbackQuery)
                {
                    if (update.CallbackQuery.Data.StartsWith("AddOrderProduct")
                    && int.TryParse(update.CallbackQuery.Data.Replace("AddOrderProduct_", ""), out int val))
                    {
                        AddOrder(update, message.Chat, products.First(a => a.Id == val));
                        _botClient.DeleteMessage(message.Chat, sentMessage.Id);
                    }
                }
            };
        }


        private async Task SelectProductToCancel(ITelegramBotClient bot, Message message)
        {
            var products = (await _dataBase.GetOrderedProducts(message.From.Username)).ToList();
            var inlineKeyboard = new InlineKeyboardMarkup(
                products.Select(product =>
                    new[] { InlineKeyboardButton.WithCallbackData($"{product.Name} ({product.Count}шт)", $"CancelOrderProduct_{product.Id}") })

            );

            var sentMessage = await bot.SendMessage(
                chatId: message.Chat,
                text: "Выберите товар для отмены",
                replyMarkup: inlineKeyboard
            );
            _onResponse[message.From.Id] = async (update) => {

                if (update.Type == UpdateType.CallbackQuery)
                {
                    if (update.CallbackQuery.Data.StartsWith("CancelOrderProduct")
                    && int.TryParse(update.CallbackQuery.Data.Replace("CancelOrderProduct_", ""), out int val))
                    {
                        await _dataBase.CancelOrder(val, message.From.Username);
                        await _botClient.DeleteMessage(message.Chat, sentMessage.Id);
                        await MyOrders(bot, message);
                        await _botClient.AnswerCallbackQuery(update.CallbackQuery.Id, text: "\n");
                    }
                }
            };
        }

        private async Task MyOrders(ITelegramBotClient bot, Message message)
        {
            var text = "У вас пока нет броней";
            var result = await _dataBase.GetOrderedProducts(message.From.Username);
            if (result.Count() > 0)
                text = "Вы забронировали:\n" + string.Join(",\n", result
                    .Select(a => $"{a.Name} ({a.Count}шт)"));

            var sentMessage = await bot.SendMessage(
                chatId: message.Chat,
                text: text
            );
        }

        private async Task HandleErrorAsync(ITelegramBotClient client, Exception exception, CancellationToken token)
        {
            Console.WriteLine($"An error occurred: {exception}");

            var userId = exception.Data["chatId"];
            var errorMessage = "An error occurred while processing your request. Please try again later.";
            await _botClient.SendMessage((ChatId)userId, errorMessage);
        }

    }
}
