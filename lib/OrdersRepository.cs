using System.Text.Json;
using Marketplace.Models;

namespace Marketplace.Lib
{
    public class OrdersRepository
    {
        private readonly string _filePath;
        private readonly object _lock = new();

        public OrdersRepository(string contentRoot)
        {
            var dataDir = Path.Combine(contentRoot, "Data");
            Directory.CreateDirectory(dataDir);
            _filePath = Path.Combine(dataDir, "orders.json");
            if (!File.Exists(_filePath)) File.WriteAllText(_filePath, "[]");
        }

        public List<Order> LoadAll()
        {
            lock (_lock)
            {
                var json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<List<Order>>(json) ?? new List<Order>();
            }
        }

        public void Save(Order order)
        {
            lock (_lock)
            {
                var orders = LoadAll();
                orders.Add(order);
                var json = JsonSerializer.Serialize(orders, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_filePath, json);
            }
        }
    }
}
