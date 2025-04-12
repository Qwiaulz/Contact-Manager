using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ContactManagerApp.Models;

namespace ContactManagerApp.Services
{
    public static class AuthService
    {
        // Явно вказуємо шлях до Data в корені проєкту
        private static readonly string UsersFilePath = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName, "Data", "users.json");

        // Метод для генерування випадкової солі
        private static string GenerateSalt()
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] saltBytes = new byte[16]; // Розмір солі, 16 байтів
                rng.GetBytes(saltBytes);
                return Convert.ToBase64String(saltBytes);
            }
        }

        // Метод для хешування пароля з сіллю
        private static string HashPassword(string password, string salt)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                // Додаємо сіль до пароля
                byte[] saltedPassword = Encoding.UTF8.GetBytes(password + salt);
                byte[] hashBytes = sha256.ComputeHash(saltedPassword);
                return Convert.ToBase64String(hashBytes); // Повертаємо хеш у вигляді строки
            }
        }

        public static bool Register(string username, string password)
        {
            var users = LoadUsers();

            if (users.Any(u => u.Username == username))
            {
                // Якщо користувач вже існує:
                return false;
            }

            // Генеруємо унікальну сіль для користувача
            string salt = GenerateSalt();

            // Хешуємо пароль з сіллю
            string hashedPassword = HashPassword(password, salt);

            users.Add(new User { Username = username, Password = hashedPassword, Salt = salt, Role = "user", IsActive = true, UserId = Guid.NewGuid().ToString() });
            SaveUsers(users);
            return true;
        }

        public static User? Login(string username, string password)
        {
            var users = LoadUsers();

            // Шукаємо користувача за логіном
            var user = users.FirstOrDefault(u => u.Username == username && u.IsActive);
            if (user == null)
                return null;

            // Хешуємо введений пароль з використанням збереженої солі
            string hashedPassword = HashPassword(password, user.Salt);

            // Порівнюємо хеш введеного пароля з хешем, збереженим у файлі
            return user.Password == hashedPassword ? user : null;
        }

        public static bool UsersExist(string username)
        {
            var users = LoadUsers();
            return users.Any(u => u.Username == username);
        }

        private static List<User> LoadUsers()
        {
            try
            {
                if (!File.Exists(UsersFilePath))
                {
                    return new List<User>(); // Якщо файл не існує, повертаємо порожній список
                }

                string json = File.ReadAllText(UsersFilePath);
                return JsonSerializer.Deserialize<List<User>>(json) ?? new List<User>();
            }
            catch (IOException ex)
            {
                // Логування помилки доступу до файлу
                LogError("Помилка при зчитуванні файлу користувачів: " + ex.Message);
                return new List<User>(); // Повертаємо порожній список при помилці
            }
            catch (JsonException ex)
            {
                // Логування помилки при десеріалізації
                LogError("Помилка при десеріалізації файлу користувачів: " + ex.Message);
                return new List<User>(); // Повертаємо порожній список при помилці
            }
        }

        private static void SaveUsers(List<User> users)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(UsersFilePath)); // на випадок, якщо папка ще не створена
                string json = JsonSerializer.Serialize(users, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(UsersFilePath, json);
            }
            catch (IOException ex)
            {
                // Логування помилки доступу до файлу
                LogError("Помилка при записі файлу користувачів: " + ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                // Логування помилки доступу
                LogError("Помилка доступу до файлу користувачів: " + ex.Message);
            }
        }

        private static void LogError(string message)
        {
            // Це місце для додавання логування помилок, наприклад, запис у файл або консоль
            System.Diagnostics.Debug.WriteLine(message);
        }
    }
}
