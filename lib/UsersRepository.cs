using System.Text.Json;
using Marketplace.Models;

namespace Marketplace.Lib
{
    public class UsersRepository
    {
        private readonly string _filePath;
        private readonly object _lock = new();

        public UsersRepository(string contentRoot)
        {
            var dataDir = Path.Combine(contentRoot, "Data");
            Directory.CreateDirectory(dataDir);
            _filePath = Path.Combine(dataDir, "users.json");
            if (!File.Exists(_filePath)) File.WriteAllText(_filePath, "[]");
        }

        public List<User> LoadAll()
        {
            lock (_lock)
            {
                var json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<List<User>>(json) ?? new List<User>();
            }
        }

        public void Save(User user)
        {
            lock (_lock)
            {
                var users = LoadAll();
                var existing = users.FirstOrDefault(u => u.Email.Equals(user.Email, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    // update
                    existing.FullName = user.FullName;
                    existing.Password = user.Password;
                    existing.Role = user.Role;
                    existing.Bio = user.Bio;
                    existing.SellerStatus = user.SellerStatus;
                    existing.AvatarInitials = user.AvatarInitials;
                    existing.JoinedDate = user.JoinedDate;
                }
                else
                {
                    users.Add(user);
                }

                var json = JsonSerializer.Serialize(users, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_filePath, json);
            }
        }
    }
}
