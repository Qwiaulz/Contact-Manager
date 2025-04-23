using ContactManagerApp.Services;
using System;
using System.Windows;
using ContactManagerApp.Models;

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
                    // Встановлюємо TokenExpiration на 5 хвилин після закриття
                    user.TokenExpiration = DateTime.UtcNow.AddMinutes(10);
                    AuthService.SaveUsers(users);
                    System.Diagnostics.Debug.WriteLine($"Updated TokenExpiration to {user.TokenExpiration} for UserId: {user.UserId}");
                }
            }
        }
    }
}