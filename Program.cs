using TelegramStore.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TelegramGameTest
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            using var file = new StreamReader(new FileStream(Path.GetFullPath("app.config.json"), FileMode.Open));
            dynamic config = JsonConvert.DeserializeObject<dynamic>(file.ReadToEnd());

            var token = (string)(config.telegram_token);
            var connectionString = (string)config.connection_string;
            var telegram = new TelegramAPI.TelegramAPI(token, new DataBase(connectionString), ((JArray)config.admins).Select(a => a.ToString()));
            telegram.Start();
            await Task.Delay(-1);
        }
    }
}