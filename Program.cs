using TelegramStore.Data;

namespace TelegramGameTest
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            var config = Configuration.Configuration.ReadFromFile(Path.GetFullPath("app.config.json"));

            var token = config["telegram_token"].Value;
            var connectionString = config["connection_string"].Value;
            var telegram = new TelegramAPI.TelegramAPI(token, new DataBase(connectionString), config["admins"].Select(a=>a.Value));
            telegram.Start();
            await Task.Delay(-1);
        }
    }
}