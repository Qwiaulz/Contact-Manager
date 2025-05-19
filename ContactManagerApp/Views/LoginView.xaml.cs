using ContactManagerApp.Services;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ContactManagerApp.Views
{
    public partial class LoginView : UserControl, INotifyPropertyChanged
    {
        private readonly SettingService _settingService;
        private string _passwordText = "";

        public event PropertyChangedEventHandler PropertyChanged;
        public event Action NavigateToRegistration;

        public string PasswordText
        {
            get => _passwordText;
            set
            {
                _passwordText = value;
                OnPropertyChanged(nameof(PasswordText));
            }
        }

        public LoginView()
        {
            InitializeComponent();
            DataContext = this;
            PasswordText = "";
            _settingService = App.SettingStorage;
            System.Diagnostics.Debug.WriteLine("LoginView initialized");

            // Перевіряємо CurrentUserId для заповнення полів
            string currentUserId = _settingService.GetSetting("CurrentUserId");
            bool wasTokenExpired = false;

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
                    // Перевіряємо, чи ініціалізований UsernameTextBox
                    if (UsernameTextBox != null)
                    {
                        UsernameTextBox.Text = user.Username;
                        PasswordBox?.Focus();
                        System.Diagnostics.Debug.WriteLine($"Auto-filled username: {user.Username}, UserId: {user.UserId}");
                        _settingService.SetCurrentUserIdLastUsed(DateTime.UtcNow);

                        // Перевіряємо, чи токен прострочений
                        string token = _settingService.GetSetting("AuthToken");
                        if (!string.IsNullOrEmpty(token) && AuthService.ValidateToken(token) == null)
                        {
                            wasTokenExpired = true;
                        }

                        if (wasTokenExpired && !_settingService.GetSessionExpiredShown())
                        {
                            _settingService.SetSessionExpiredShown(true);
                            ShowCustomDialog("SessionExpired", user?.Language ?? LocalizationManager.CurrentLanguage);
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("UsernameTextBox is null during auto-fill attempt");
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

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            PasswordText = PasswordBox.Password;
            System.Diagnostics.Debug.WriteLine($"Password changed: Length = {PasswordText.Length}");
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text;
            string password = PasswordBox.Password;

            UsernameError.Visibility = Visibility.Collapsed;
            PasswordError.Visibility = Visibility.Collapsed;

            bool hasErrors = false;

            if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
            {
                UsernameError.Text = "Username must be at least 3 characters long.";
                UsernameError.Visibility = Visibility.Visible;
                System.Diagnostics.Debug.WriteLine("Login failed: Username too short or empty");
                hasErrors = true;
            }

            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            {
                PasswordError.Text = "Password must be at least 8 characters long.";
                PasswordError.Visibility = Visibility.Visible;
                System.Diagnostics.Debug.WriteLine("Login failed: Password too short or empty");
                hasErrors = true;
            }

            if (hasErrors)
            {
                return;
            }

            var user = AuthService.Login(username, password);
            if (user != null)
            {
                if (string.IsNullOrEmpty(user.UserId))
                {
                    ShowCustomDialog("UserIdEmptyError", user?.Language ?? LocalizationManager.CurrentLanguage);
                    System.Diagnostics.Debug.WriteLine("Login failed: UserId is empty");
                    return;
                }

                ThemeManager.ApplyTheme(user.Theme);
                System.Diagnostics.Debug.WriteLine($"Applied settings for login: Theme={user.Theme}");

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
                var window = Window.GetWindow(this);
                if (window != null)
                {
                    window.Close();
                }
            }
            else
            {
                PasswordError.Text = "Invalid username or password."; // Замінюємо локалізоване повідомлення на статичне
                PasswordError.Visibility = Visibility.Visible;
                System.Diagnostics.Debug.WriteLine($"Login failed: Invalid credentials for {username}");
            }
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToRegistration?.Invoke();
        }

        private void ShowCustomDialog(string key, string userLanguage)
        {
            // Оновлюємо мову для діалогу
            LocalizationManager.CurrentLanguage = userLanguage ?? LocalizationManager.CurrentLanguage;

            var dialog = new CustomConfirmationDialog
            {
                Title = LocalizationManager.GetString("Error"),
                Message = LocalizationManager.GetString(key),
                ConfirmButtonText = LocalizationManager.GetString("OK"),
                CancelButtonText = ""
            };

            var window = new Window
            {
                AllowsTransparency = true,
                Content = dialog,
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize,
                Background = null
            };

            dialog.DialogResult += (s, result) => { };
            window.ShowDialog();
        }
    }
}