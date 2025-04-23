using ContactManagerApp.Services;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ContactManagerApp.Views
{
    public partial class LoginView : Window
    {
        private readonly SettingService _settingService;

        public LoginView()
        {
            InitializeComponent();
            _settingService = App.SettingStorage;
            System.Diagnostics.Debug.WriteLine("LoginView initialized");

            string token = _settingService.GetSetting("AuthToken");
            bool wasTokenExpired = false;
            if (!string.IsNullOrEmpty(token))
            {
                var user = AuthService.ValidateToken(token);
                if (user != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Auto-login successful: User {user.Username}, UserId: {user.UserId}");
                    App.CurrentUser = user;

                    // Застосовуємо налаштування мови і теми
                    LocalizationManager.CurrentLanguage = user.Language;
                    ThemeManager.ApplyTheme(user.Theme);
                    System.Diagnostics.Debug.WriteLine($"Applied settings for auto-login: Language={user.Language}, Theme={user.Theme}");

                    var contactService = new ContactService(user.ContactFolderCode);
                    MainView mainView = new MainView(contactService);
                    mainView.Show();
                    Close();
                    return;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Auto-login failed: Invalid or expired token");
                    wasTokenExpired = true;
                }
            }

            string currentUserId = _settingService.GetSetting("CurrentUserId");
            if (!string.IsNullOrEmpty(currentUserId))
            {
                DateTime? lastUsed = _settingService.GetCurrentUserIdLastUsed();
                if (lastUsed.HasValue && (DateTime.UtcNow - lastUsed.Value).TotalDays > 1)
                {
                    System.Diagnostics.Debug.WriteLine($"CurrentUserId {currentUserId} expired (last used: {lastUsed.Value})");
                    _settingService.SetSetting("CurrentUserId", null);
                    _settingService.SetCurrentUserIdLastUsed(null);
                    currentUserId = null;
                }
            }

            if (!string.IsNullOrEmpty(currentUserId))
            {
                var users = AuthService.LoadUsers();
                var user = users.FirstOrDefault(u => u.UserId == currentUserId && u.IsActive);
                if (user != null)
                {
                    UsernameTextBox.Text = user.Username;
                    PasswordBox.Focus();
                    System.Diagnostics.Debug.WriteLine($"Auto-filled username: {user.Username}, UserId: {user.UserId}");
                    _settingService.SetCurrentUserIdLastUsed(DateTime.UtcNow);
                    if (wasTokenExpired && !_settingService.GetSessionExpiredShown())
                    {
                        _settingService.SetSessionExpiredShown(true);
                        MessageBox.Show(LocalizationManager.GetString("Your session has expired. Please enter your password to continue.",
                            "Session Expired", MessageBoxButton.OK, MessageBoxImage.Information));
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Invalid CurrentUserId: {currentUserId}, clearing it");
                    _settingService.SetSetting("CurrentUserId", null);
                    _settingService.SetCurrentUserIdLastUsed(null);
                    _settingService.SetSessionExpiredShown(false);
                }
            }
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text;
            string password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show(LocalizationManager.GetString("EmptyFieldsError"), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine("Login failed: Empty username or password");
                return;
            }

            var user = AuthService.Login(username, password);
            if (user != null)
            {
                if (string.IsNullOrEmpty(user.UserId))
                {
                    MessageBox.Show("Error: UserId is empty", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    System.Diagnostics.Debug.WriteLine("Login failed: UserId is empty");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"Login successful: User {username}, UserId: {user.UserId}");
                MessageBox.Show(LocalizationManager.GetString("LoginSuccess") ?? "Успішний вхід!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                // Застосовуємо налаштування мови і теми
                LocalizationManager.CurrentLanguage = user.Language;
                ThemeManager.ApplyTheme(user.Theme);
                System.Diagnostics.Debug.WriteLine($"Applied settings for login: Language={user.Language}, Theme={user.Theme}");

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
                var contactService = new ContactService(user.ContactFolderCode);
                MainView mainView = new MainView(contactService);
                mainView.Show();
                Close();
            }
            else
            {
                MessageBox.Show(LocalizationManager.GetString("InvalidCredentialsError"), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"Login failed: Invalid credentials for {username}");
            }
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            RegistrationView registrationView = new RegistrationView();
            registrationView.Show();
            Close();
            System.Diagnostics.Debug.WriteLine("Navigated to RegistrationView");
        }
    }
}