using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ContactManagerApp.Models;

namespace ContactManagerApp.Services
{
    public static class ContactService
    {
        private static readonly string ContactsFilePath =
            Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName, "Data",
                "contacts.json");

        public static List<Contact> GetContactsByUserId(string userId)
        {
            var contacts = LoadContacts();
    
            // Логування для перевірки
            System.Diagnostics.Debug.WriteLine($"Фільтрація за UserId: {userId}");
    
            var filteredContacts = contacts.Where(c => c.UserId == userId).ToList();
    
            // Логування кількості відфільтрованих контактів
            System.Diagnostics.Debug.WriteLine($"Знайдено {filteredContacts.Count} контактів для користувача {userId}");
    
            return filteredContacts;
        }
        
        private static void LogError(string message)
        {
            // Логування помилок - тут ми виводимо в консоль
            System.Diagnostics.Debug.WriteLine(message);
        }

        private static List<Contact> LoadContacts()
        {
            try
            {
                if (!File.Exists(ContactsFilePath))
                {
                    // Якщо файл не існує, повертаємо порожній список
                    return new List<Contact>();
                }

                string json = File.ReadAllText(ContactsFilePath);

                // Перевірка на порожній файл
                if (string.IsNullOrWhiteSpace(json))
                {
                    // Логування, що файл порожній
                    LogError("Файл контактів порожній. Створення порожнього списку.");
                    return new List<Contact>(); // Якщо файл порожній, повертаємо порожній список
                }

                // Десеріалізація списку контактів з перевіркою на null
                return JsonSerializer.Deserialize<List<Contact>>(json) ?? new List<Contact>();
            }
            catch (IOException ex)
            {
                // Логування помилки доступу до файлу
                LogError("Помилка при зчитуванні файлу контактів: " + ex.Message);
                return new List<Contact>(); // Повертаємо порожній список при помилці
            }
            catch (JsonException ex)
            {
                // Логування помилки при десеріалізації
                LogError("Помилка при десеріалізації файлу контактів: " + ex.Message);
                return new List<Contact>(); // Повертаємо порожній список при помилці
            }
        }
    }
}