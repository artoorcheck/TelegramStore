using System;
using System.Linq;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramStore.Data;
using TelegramStore.Data.Models;

namespace TelegramGameTest.TelegramAPI
{
    public class TelegramAdminAPI
    {
        private ITelegramBotClient _botClient;

        private DataBase _dataBase;

        private Dictionary<long, Action<Update>> _onResponse;

        private string[] _admins = new[] { "Artooorcheck" };

        public TelegramAdminAPI(ITelegramBotClient bot, DataBase db, Dictionary<long, Action<Update>> onResponse)
        {
            _botClient = bot;
            _dataBase = db;
            _onResponse = onResponse;
        }

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.Message)
            {
                var message = update.Message;
                if (message.Text.StartsWith("/admin"))
                {
                    await SendReplyKeyboard(_botClient, message.Chat.Id);
                    _dataBase.AddAdmin(message.From.Username, message.Chat.Id);
                    return;
                }
                if (message.Text == "Все брони")
                {
                    await AllOrders(_botClient, message);
                    return;
                }
                if (message.Text == "Добавить товар")
                {
                    await AddProduct(_botClient, message);
                    return;
                }
                if (message.Text == "Удалить товар")
                {
                    await RemoveProduct(_botClient, message);
                    return;
                }
                return;
            }
        }

        private async Task AddProduct(ITelegramBotClient botClient, Message message)
        {
            var products = (await _dataBase.GetAllProducts());
            var productsView = products.Select(a=> ($"{a.Name} ({a.Count}шт)", $"AddProduct_{a.Id}")).ToList();
            productsView.Add(($"Новый товар", $"AddProduct_{-1}"));
            var inlineKeyboard = new InlineKeyboardMarkup(
                productsView.Select(product =>
                    new[] { InlineKeyboardButton.WithCallbackData(product.Item1, product.Item2) })
            );

            var sentMessage = await botClient.SendMessage(
                chatId: message.Chat,
                text: "Выберите товар",
                replyMarkup: inlineKeyboard
            );
            _onResponse[message.From.Id] = (update) => {

                if (update.Type == UpdateType.CallbackQuery)
                {
                    if (update.CallbackQuery.Data.StartsWith("AddProduct")
                    && int.TryParse(update.CallbackQuery.Data.Replace("AddProduct_", ""), out int val))
                    {
                        if (val < 0)
                        {
                            CreateProduct(update, message.Chat);
                        }
                        else
                        {
                            AddProduct(update, message.Chat, products.First(a => a.Id == val));
                        }
                        _botClient.DeleteMessage(message.Chat, sentMessage.Id);
                    }
                }
            };
        }

        private async Task RemoveProduct(ITelegramBotClient botClient, Message message)
        {
            var products = (await _dataBase.GetAllProducts());
            var productsView = products.Select(a => ($"{a.Name} ({a.Count}шт)", $"RemoveProduct_{a.Id}")).ToList();
            var inlineKeyboard = new InlineKeyboardMarkup(
                productsView.Select(product =>
                    new[] { InlineKeyboardButton.WithCallbackData(product.Item1, product.Item2) })
            );

            var sentMessage = await botClient.SendMessage(
                chatId: message.Chat,
                text: "Выберите товар для удаления",
                replyMarkup: inlineKeyboard
            );
            _onResponse[message.From.Id] = (update) => {

                if (update.Type == UpdateType.CallbackQuery)
                {
                    if (update.CallbackQuery.Data.StartsWith("RemoveProduct")
                    && int.TryParse(update.CallbackQuery.Data.Replace("RemoveProduct_", ""), out int val))
                    {
                        RemoveProduct(update, message.Chat, products.First(a => a.Id == val));
                        _botClient.DeleteMessage(message.Chat, sentMessage.Id);
                    }
                }
            };
        }

        private async Task AddProduct(Update update, Chat chat, Product product)
        {
            _onResponse[update.CallbackQuery.From.Id] = async (update) => {
                var message = update.Message;
                if (uint.TryParse(message?.Text, out uint val))
                {
                    await _dataBase.AddProduct(product.Id, (int)val);
                    await _botClient.SendMessage(
                        chatId: chat,
                        text: $"Добавлено {product.Name} - {val}шт"
                    );
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

        private async Task RemoveProduct(Update update, Chat chat, Product product)
        {
            _onResponse[update.CallbackQuery.From.Id] = async (update) => {
                var message = update.Message;
                if (uint.TryParse(message?.Text, out uint val))
                {
                    await _dataBase.AddProduct(product.Id, -(int)val);
                    await _botClient.SendMessage(
                        chatId: chat,
                        text: $"Удалено {product.Name} - {val}шт"
                    );
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
                text: $"Введите количество {product.Name} для удаления:"
            );
            await _botClient.AnswerCallbackQuery(update.CallbackQuery.Id, text: "Введите количество:");
        }

        private async Task CreateProduct(Update update, Chat chat)
        {
                _onResponse[update.CallbackQuery.From.Id] = async (update) => {
                    var message = update.Message;
                    if (message == null)
                        return;

                    var data = message.Text.Split("\n");

                    foreach (var product in data)
                    {
                        var pair = product.Split(":");
                        try
                        {
                            if (uint.TryParse(pair[1], out uint val))
                            {
                                _dataBase.CreateProduct(pair[0], (int)val);
                            }
                        }
                        catch { }
                    }
                };
            await _botClient.SendMessage(
                chatId: chat,
                text: $"Введите продукты в формате:\nName1:Count1\nName2:Count1\n..."
            );
            await _botClient.AnswerCallbackQuery(update.CallbackQuery.Id, text: "Введите количество:");
        }

        private async Task SelectOrderToClose(ITelegramBotClient bot, Message message, string userId)
        {
            var products = (await _dataBase.GetOrderedProducts(userId)).Select(a=>($"{a.Name} ({a.Count}шт)", $"CloseOrderProduct_{a.Id}")).ToList();
            products.Add(($"Закрыть все", $"CloseOrderProduct_{-1}"));

            var inlineKeyboard = new InlineKeyboardMarkup(
                products.Select(product =>
                    new[] { InlineKeyboardButton.WithCallbackData(product.Item1, product.Item2) })

            );

            var sentMessage = await bot.SendMessage(
                chatId: message.Chat,
                text: "Выберите закрытые брони",
                replyMarkup: inlineKeyboard
            );
            _onResponse[message.From.Id] = async (update) => {

                if (update.Type == UpdateType.CallbackQuery)
                {
                    if (update.CallbackQuery.Data.StartsWith("CloseOrderProduct")
                    && int.TryParse(update.CallbackQuery.Data.Replace("CloseOrderProduct_", ""), out int val))
                    {
                        await _dataBase.CloseOrder(val, userId);
                        await _botClient.AnswerCallbackQuery(update.CallbackQuery.Id, text: "\n");
                    }
                }
            };
        }

        private async Task AllOrders(ITelegramBotClient botClient, Message message)
        {
            var clients = (await _dataBase.GetClients()).ToList();
            var inlineKeyboard = new InlineKeyboardMarkup(
                clients.Select(cl =>
                    new[] { InlineKeyboardButton.WithCallbackData($"{cl.Name} ({cl.Username})", $"ClientOrder_{cl.Username}") })

            );

            var sentMessage = await botClient.SendMessage(
                chatId: message.Chat,
                text: "Выберите товар для отмены",
                replyMarkup: inlineKeyboard
            );

            _onResponse[message.From.Id] = async (update) => {

                if (update.Type == UpdateType.CallbackQuery)
                {
                    var query = update.CallbackQuery;
                    if (query.Data.StartsWith("ClientOrder"))
                    {
                        var userId = query.Data.Replace("ClientOrder_", "");
                        await SelectOrderToClose(botClient, message, userId);
                        await _botClient.AnswerCallbackQuery(update.CallbackQuery.Id, text: "\n");
                        await _botClient.DeleteMessage(message.Chat, sentMessage.Id);
                    }
                }
            };
        }

        private async Task SendReplyKeyboard(ITelegramBotClient bot, long chatId)
        {
            var replyKeyboard = new ReplyKeyboardMarkup(new[]
               {
                    new KeyboardButton[] { "Добавить товар"},
                    new KeyboardButton[] { "Все брони"},
                    new KeyboardButton[] { "Удалить товар" },
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
    }
}
