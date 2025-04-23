using System;
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
        private static readonly string UsersFilePath = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName, "Data", "Users", "users.json");

        private static string GenerateSalt()
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] saltBytes = new byte[16];
                rng.GetBytes(saltBytes);
                return Convert.ToBase64String(saltBytes);
            }
        }

        private static string HashPassword(string password, string salt)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] saltedPassword = Encoding.UTF8.GetBytes(password + salt);
                byte[] hashBytes = sha256.ComputeHash(saltedPassword);
                return Convert.ToBase64String(hashBytes);
            }
        }

        private static string GenerateToken()
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] tokenBytes = new byte[32];
                rng.GetBytes(tokenBytes);
                return Convert.ToBase64String(tokenBytes);
            }
        }

        public static bool Register(string username, string password, out User newUser)
        {
            newUser = null;
            var users = LoadUsers();

            if (users.Any(u => u.Username == username))
            {
                System.Diagnostics.Debug.WriteLine($"Registration failed: Username {username} already exists");
                return false;
            }

            string salt = GenerateSalt();
            string hashedPassword = HashPassword(password, salt);

            newUser = new User
            {
                Username = username,
                Password = hashedPassword,
                Salt = salt,
                Role = "user",
                IsActive = true,
                Language = "uk",
                Theme = "Light"
            };
            users.Add(newUser);

            string contactFolderPath = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName, "Data", "Contacts", $"user_{newUser.ContactFolderCode}");
            try
            {
                Directory.CreateDirectory(contactFolderPath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create contact folder: {ex.Message}");
                return false;
            }

            SaveUsers(users);
            System.Diagnostics.Debug.WriteLine($"Registered user: {username}, UserId: {newUser.UserId}, ContactFolderCode: {newUser.ContactFolderCode}");
            return true;
        }

        public static User? Login(string username, string password)
        {
            var users = LoadUsers();

            var user = users.FirstOrDefault(u => u.Username == username && u.IsActive);
            if (user == null)
            {
                System.Diagnostics.Debug.WriteLine($"Login failed: User {username} not found or inactive");
                return null;
            }

            string hashedPassword = HashPassword(password, user.Salt);
            if (user.Password == hashedPassword)
            {
                user.Token = GenerateToken();
                user.TokenExpiration = DateTime.UtcNow.AddMinutes(5);
                SaveUsers(users);
                System.Diagnostics.Debug.WriteLine($"Login successful: User {username}, UserId: {user.UserId}, Token: {user.Token}");
                return user;
            }
            System.Diagnostics.Debug.WriteLine($"Login failed: Incorrect password for {username}");
            return null;
        }

        public static User? ValidateToken(string token)
        {
            var users = LoadUsers();
            var user = users.FirstOrDefault(u => u.Token == token && u.IsActive && u.TokenExpiration > DateTime.UtcNow);
            if (user == null)
            {
                System.Diagnostics.Debug.WriteLine($"Token validation failed: Token {token} not found, expired, or user inactive");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Token validation successful: User {user.Username}, UserId: {user.UserId}");
            }
            return user;
        }

        public static bool UsersExist(string username)
        {
            var users = LoadUsers();
            bool exists = users.Any(u => u.Username == username);
            System.Diagnostics.Debug.WriteLine($"Checked if user {username} exists: {exists}");
            return exists;
        }

        public static bool ChangePassword(string userId, string oldPassword, string newPassword)
        {
            var users = LoadUsers();
            var user = users.FirstOrDefault(u => u.UserId == userId && u.IsActive);
            if (user == null)
            {
                System.Diagnostics.Debug.WriteLine($"Change password failed: UserId {userId} not found");
                return false;
            }

            string hashedOldPassword = HashPassword(oldPassword, user.Salt);
            if (user.Password != hashedOldPassword)
            {
                System.Diagnostics.Debug.WriteLine($"Change password failed: Incorrect old password for UserId {userId}");
                return false;
            }

            string newSalt = GenerateSalt();
            string hashedNewPassword = HashPassword(newPassword, newSalt);

            user.Salt = newSalt;
            user.Password = hashedNewPassword;
            user.Token = null;
            user.TokenExpiration = null;
            SaveUsers(users);
            System.Diagnostics.Debug.WriteLine($"Password changed for UserId: {userId}");
            return true;
        }

        public static bool DeleteAccount(string userId)
        {
            var users = LoadUsers();
            var user = users.FirstOrDefault(u => u.UserId == userId);
            if (user == null)
            {
                System.Diagnostics.Debug.WriteLine($"Delete account failed: UserId {userId} not found in users.json");
                return false;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine($"Found user: Username={user.Username}, UserId={user.UserId}, ContactFolderCode={user.ContactFolderCode}");

                var contactService = new ContactService(user.ContactFolderCode);
                var contacts = contactService.GetAllContacts().ToList();
                System.Diagnostics.Debug.WriteLine($"Found {contacts.Count} contacts to delete for UserId: {userId}");
                foreach (var contact in contacts)
                {
                    contactService.DeleteContact(contact);
                }

                string contactFolderPath = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName, "Data", "Contacts", $"user_{user.ContactFolderCode}");
                if (Directory.Exists(contactFolderPath))
                {
                    Directory.Delete(contactFolderPath, true);
                    System.Diagnostics.Debug.WriteLine($"Deleted contact folder: {contactFolderPath}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Contact folder not found: {contactFolderPath}");
                }

                user.Token = null;
                user.TokenExpiration = null;
                user.IsActive = false;

                users.Remove(user);
                SaveUsers(users);
                System.Diagnostics.Debug.WriteLine($"Account deleted: UserId {userId}");
                return true;
            }
            catch (IOException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to delete account due to file operation error: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Unexpected error during account deletion: {ex.Message}");
                return false;
            }
        }

        public static void UpdateUserSettings(string userId, string language, string theme)
        {
            var users = LoadUsers();
            var user = users.FirstOrDefault(u => u.UserId == userId && u.IsActive);
            if (user == null)
            {
                System.Diagnostics.Debug.WriteLine($"Update settings failed: UserId {userId} not found or inactive");
                return;
            }

            user.Language = language;
            user.Theme = theme;
            try
            {
                SaveUsers(users);
                System.Diagnostics.Debug.WriteLine($"Settings updated for UserId: {userId}, Language: {language}, Theme: {theme}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save user settings: {ex.Message}");
            }
        }

        public static List<User> LoadUsers()
        {
            try
            {
                if (!File.Exists(UsersFilePath))
                {
                    System.Diagnostics.Debug.WriteLine($"Users file not found at {UsersFilePath}");
                    return new List<User>();
                }

                byte[] encryptedJson = File.ReadAllBytes(UsersFilePath);
                byte[] jsonBytes = ProtectedData.Unprotect(encryptedJson, null, DataProtectionScope.CurrentUser);
                string json = Encoding.UTF8.GetString(jsonBytes);
                var users = JsonSerializer.Deserialize<List<User>>(json) ?? new List<User>();
                System.Diagnostics.Debug.WriteLine($"Loaded {users.Count} users from {UsersFilePath}");
                return users;
            }
            catch (IOException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading users: {ex.Message}");
                return new List<User>();
            }
            catch (CryptographicException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error decrypting users: {ex.Message}");
                return new List<User>();
            }
            catch (JsonException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deserializing users: {ex.Message}");
                return new List<User>();
            }
        }

        public static void SaveUsers(List<User> users)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(UsersFilePath));
                string json = JsonSerializer.Serialize(users, new JsonSerializerOptions { WriteIndented = true });
                byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
                byte[] encryptedJson = ProtectedData.Protect(jsonBytes, null, DataProtectionScope.CurrentUser);
                File.WriteAllBytes(UsersFilePath, encryptedJson);
                System.Diagnostics.Debug.WriteLine($"Saved {users.Count} users to {UsersFilePath}");
            }
            catch (IOException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving users: {ex.Message}");
            }
            catch (CryptographicException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error encrypting users: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving users: {ex.Message}");
            }
        }
    }
}