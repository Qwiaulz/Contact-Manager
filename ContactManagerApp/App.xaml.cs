using ContactManagerApp.Services;
using System;
using System.Windows;
using ContactManagerApp.Models;
using ContactManagerApp.Views;

namespace ContactManagerApp
{
    public partial class App : Application
    {
        public static User CurrentUser { get; set; }
        public static ContactService CurrentContactService { get; set; }
        public static void InitializeContactService(string userContactFolderCode)
        {
            CurrentContactService = new ContactService(userContactFolderCode);
        }

        public static SettingService _settingStorage;

        public static SettingService SettingStorage
        {
            get
            {
                if (_settingStorage == null)
                {
                    _settingStorage = new SettingService();
                }
                return _settingStorage;
            }
            set => _settingStorage = value;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            System.Diagnostics.Debug.WriteLine("Application started");

            // Перевіряємо токен для автоматичного входу
            string token = SettingStorage.GetSetting("AuthToken");
            if (!string.IsNullOrEmpty(token))
            {
                var user = AuthService.ValidateToken(token);
                if (user != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Auto-login successful: User {user.Username}, UserId: {user.UserId}");
                    CurrentUser = user;

                    ThemeManager.ApplyTheme(user.Theme);
                    System.Diagnostics.Debug.WriteLine($"Applied settings for auto-login: Theme={user.Theme}");

                    var contactService = new ContactService(user.ContactFolderCode);
                    MainView mainView = new MainView(contactService);
                    mainView.Show();
                    return; // Завершуємо метод, щоб не відкривати AuthView
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Auto-login failed: Invalid or expired token");
                }
            }

            // Якщо автоматичний вхід не вдався, відкриваємо AuthView
            AuthView authView = new AuthView();
            authView.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            System.Diagnostics.Debug.WriteLine("Application exiting");

            // Записуємо час закриття програми
            SettingStorage.SetAppLastClosed(DateTime.UtcNow);

            // Оновлюємо TokenExpiration, якщо токен ще дійсний і RememberMe активовано
            string token = SettingStorage.GetSetting("AuthToken");
            if (!string.IsNullOrEmpty(token))
            {
                var users = AuthService.LoadUsers();
                var user = users.FirstOrDefault(u => u.Token == token && u.IsActive);
                if (user != null && user.TokenExpiration > DateTime.UtcNow)
                {
                    user.TokenExpiration = DateTime.UtcNow.AddDays(1);
                    AuthService.SaveUsers(users);
                    System.Diagnostics.Debug.WriteLine($"Updated TokenExpiration to {user.TokenExpiration} for UserId: {user.UserId}");
                }
            }
        }
    }
}