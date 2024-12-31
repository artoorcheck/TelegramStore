using Npgsql;
using System.Collections;
using System.Data;
using Telegram.Bot.Types;
using TelegramStore.Data.Models;

namespace TelegramStore.Data
{
    public class DataBase
    {

        private NpgsqlDataSource _dataSource;

        public DataBase(string connString)
        {
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(connString);
            _dataSource = dataSourceBuilder.Build();
        }

        public async Task<IEnumerable<Product>> GetProducts()
        {
            var products = new List<Product>();
            using (var conn = await _dataSource.OpenConnectionAsync())
            {
                await using (var cmd = new NpgsqlCommand("SELECT * FROM product_remains() WHERE remains > 0", conn))
                await using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                        products.Add(new Product
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            Count = reader.GetInt32(2)
                        });
                }
            }
            return products;
        }


        public async Task<IEnumerable<Product>> GetAllProducts()
        {
            var products = new List<Product>();
            using (var conn = await _dataSource.OpenConnectionAsync())
            {
                await using (var cmd = new NpgsqlCommand("SELECT id, product_name, product_count FROM products", conn))
                await using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                        products.Add(new Product
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            Count = reader.GetInt32(2)
                        });
                }
            }
            return products;
        }


        public async Task AddProduct(int productId, int count)
        {
            using (var conn = await _dataSource.OpenConnectionAsync())
            {
                await using (var cmd = new NpgsqlCommand("UPDATE products SET product_count = product_count + @count WHERE id = @id", conn))
                {
                    cmd.Parameters.AddWithValue("count", count);
                    cmd.Parameters.AddWithValue("id", productId);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task CreateProduct(string name, int count)
        {
            using (var conn = await _dataSource.OpenConnectionAsync())
            {
                await using (var cmd = new NpgsqlCommand("INSERT INTO products(product_name, product_count) VALUES(@name, @count)", conn))
                {
                    cmd.Parameters.AddWithValue("count", count);
                    cmd.Parameters.AddWithValue("name", name);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<bool> AddOrder(int productId, string username, string userId, int count)
        {
            var products = new List<Product>();
            using (var conn = await _dataSource.OpenConnectionAsync())
            {
                await using (var cmd = new NpgsqlCommand("SELECT public.add_order(@user_id, @username, @prod_id, @count)", conn))
                {
                        cmd.Parameters.AddWithValue("user_id", userId);
                        cmd.Parameters.AddWithValue("username", username);
                        cmd.Parameters.AddWithValue("count", count);
                        cmd.Parameters.AddWithValue("prod_id", productId);
                    await using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                            return reader.GetBoolean(0);
                    }
                }
            }
            return false;
        }


        public async Task CancelOrder(int productId, string userId)
        {
            var products = new List<Product>();
            using (var conn = await _dataSource.OpenConnectionAsync())
            {
                await using (var cmd = new NpgsqlCommand("SELECT * from cancel_order(@user_id, @prod_id)", conn))
                {
                    cmd.Parameters.AddWithValue("user_id", userId);
                    cmd.Parameters.AddWithValue("prod_id", productId);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task CloseOrder(int productId, string userId)
        {
            var products = new List<Product>();
            using (var conn = await _dataSource.OpenConnectionAsync())
            {
                await using (var cmd = new NpgsqlCommand("SELECT close_order(@user_id, @prod_id)", conn))
                {
                    cmd.Parameters.AddWithValue("user_id", userId);
                    cmd.Parameters.AddWithValue("prod_id", productId);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<IEnumerable<Product>> GetOrderedProducts(string userId)
        {
            var products = new List<Product>();
            using (var conn = await _dataSource.OpenConnectionAsync())
            {
                await using (var cmd = new NpgsqlCommand("SELECT product_id, product_name, sum FROM v_orders WHERE client_tg_id = @user_id", conn))
                {
                    cmd.Parameters.AddWithValue("user_id", userId);
                    await using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                            products.Add(new Product
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                Count = reader.GetInt32(2)
                            });
                    }
                }
            }
            return products;
        }


        public async Task<IEnumerable<(string, long)>> GetAdmins()
        {
            var admins = new List<(string, long)>();
            using (var conn = await _dataSource.OpenConnectionAsync())
            {
                await using (var cmd = new NpgsqlCommand("select username, chat_id from user_admin", conn))
                {
                    await using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                            admins.Add((reader.GetString(0), reader.GetInt64(1)));
                    }
                }
            }
            return admins;
        }


        public async Task<IEnumerable<Client>> GetClients()
        {
            var clients = new List<Client>();
            using (var conn = await _dataSource.OpenConnectionAsync())
            {
                await using (var cmd = new NpgsqlCommand("SELECT DISTINCT client_id, username, client_tg_id FROM v_orders", conn))
                {
                    await using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                            clients.Add(new Client { 
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                Username = reader.GetString(2)
                            });
                    }
                }
            }
            return clients;
        }

        public async Task AddAdmin(string? username, long chatId)
        {
            var products = new List<Product>();
            using (var conn = await _dataSource.OpenConnectionAsync())
            {
                await using (var cmd = new NpgsqlCommand("INSERT INTO user_admin(username, chat_id) values(@user_id, @chatId)", conn))
                {
                    cmd.Parameters.AddWithValue("user_id", username);
                    cmd.Parameters.AddWithValue("chatId", chatId);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }
    }
}
