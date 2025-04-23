using ContactManagerApp.Services;
using System;
using System.Windows;
using System.Windows.Controls;
using ContactManagerApp.Models;

namespace ContactManagerApp.Views
{
    public partial class RegistrationView : Window
    {
        private readonly SettingService _settingService;

        public RegistrationView()
        {
            InitializeComponent();
            _settingService = App.SettingStorage;
            System.Diagnostics.Debug.WriteLine("RegistrationView initialized");
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text;
            string password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show(LocalizationManager.GetString("AllFieldsRequired"), LocalizationManager.GetString("Error"), MessageBoxButton.OK, MessageBoxImage.Warning);
                System.Diagnostics.Debug.WriteLine("Registration failed: Empty username or password");
                return;
            }

            if (username.Length < 3)
            {
                MessageBox.Show(LocalizationManager.GetString("InvalidCredentialsError"), LocalizationManager.GetString("Error"), MessageBoxButton.OK, MessageBoxImage.Warning);
                System.Diagnostics.Debug.WriteLine("Registration failed: Username too short");
                return;
            }

            if (password.Length < 8)
            {
                MessageBox.Show(LocalizationManager.GetString("PasswordMinLength"), LocalizationManager.GetString("Error"), MessageBoxButton.OK, MessageBoxImage.Warning);
                System.Diagnostics.Debug.WriteLine("Registration failed: Password too short");
                return;
            }

            if (username.Contains(" ") || password.Contains(" "))
            {
                MessageBox.Show(LocalizationManager.GetString("InvalidCredentialsError"), LocalizationManager.GetString("Error"), MessageBoxButton.OK, MessageBoxImage.Warning);
                System.Diagnostics.Debug.WriteLine("Registration failed: Username or password contains spaces");
                return;
            }

            if (AuthService.UsersExist(username))
            {
                MessageBox.Show(LocalizationManager.GetString("UserExistsError"), LocalizationManager.GetString("Error"), MessageBoxButton.OK, MessageBoxImage.Warning);
                System.Diagnostics.Debug.WriteLine($"Registration failed: Username {username} already exists");
                return;
            }

            if (AuthService.Register(username, password, out User newUser))
            {
                System.Diagnostics.Debug.WriteLine($"Registration successful: User {username}, UserId: {newUser.UserId}");
                MessageBox.Show(LocalizationManager.GetString("RegistrationSuccess") ?? "Registration successful!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                var user = AuthService.Login(username, password);
                if (user != null)
                {
                    if (RememberMeCheckBox.IsChecked == true)
                    {
                        _settingService.SetSetting("AuthToken", user.Token);
                        _settingService.SetSetting("CurrentUserId", user.UserId);
                        _settingService.SetCurrentUserIdLastUsed(DateTime.UtcNow);
                        System.Diagnostics.Debug.WriteLine($"Saved CurrentUserId: {user.UserId}, AuthToken: {user.Token}");
                    }
                    else
                    {
                        _settingService.SetSetting("AuthToken", null);
                        _settingService.SetSetting("CurrentUserId", null);
                        _settingService.SetCurrentUserIdLastUsed(null);
                        System.Diagnostics.Debug.WriteLine("Cleared CurrentUserId and AuthToken");
                    }
                    _settingService.SetSessionExpiredShown(false);

                    App.CurrentUser = user;

                    // Застосовуємо налаштування мови і теми
                    LocalizationManager.CurrentLanguage = user.Language;
                    ThemeManager.ApplyTheme(user.Theme);
                    System.Diagnostics.Debug.WriteLine($"Applied settings for new user: Language={user.Language}, Theme={user.Theme}");

                    var contactService = new ContactService(user.ContactFolderCode);
                    MainView mainView = new MainView(contactService);
                    mainView.Show();
                    Close();
                }
                else
                {
                    MessageBox.Show("Error: Failed to log in after registration", LocalizationManager.GetString("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                    System.Diagnostics.Debug.WriteLine("Failed to log in after registration");
                }
            }
            else
            {
                MessageBox.Show(LocalizationManager.GetString("RegistrationFailed") ?? "Registration failed. Please try again.", LocalizationManager.GetString("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine("Registration failed: Unknown error");
            }
        }

        private void LoginLink_Click(object sender, RoutedEventArgs e)
        {
            LoginView loginView = new LoginView();
            loginView.Show();
            Close();
            System.Diagnostics.Debug.WriteLine("Navigated to LoginView");
        }
    }
}