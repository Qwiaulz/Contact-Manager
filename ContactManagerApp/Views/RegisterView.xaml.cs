using ContactManagerApp.Services;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using ContactManagerApp.Models;

namespace ContactManagerApp.Views
{
    public partial class RegistrationView : UserControl, INotifyPropertyChanged
    {
        private readonly SettingService _settingService;
        private string _passwordText = "";

        public event PropertyChangedEventHandler PropertyChanged;
        public event Action NavigateToLogin;

        public string PasswordText
        {
            get => _passwordText;
            set
            {
                _passwordText = value;
                OnPropertyChanged(nameof(PasswordText));
            }
        }

        public RegistrationView()
        {
            InitializeComponent();
            DataContext = this;
            PasswordText = "";
            _settingService = App.SettingStorage;
            System.Diagnostics.Debug.WriteLine("RegistrationView initialized");
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

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
{
    string username = UsernameTextBox.Text;
    string name = NameTextBox.Text; // Отримуємо ім’я
    string password = PasswordBox.Password;

    UsernameError.Visibility = Visibility.Collapsed;
    NameError.Visibility = Visibility.Collapsed; // Очищаємо помилки для імені
    PasswordError.Visibility = Visibility.Collapsed;

    string usernameError = null;
    string passwordError = null;

    if (string.IsNullOrWhiteSpace(username))
    {
        usernameError = "Username cannot be empty.";
        System.Diagnostics.Debug.WriteLine("Registration failed: Empty username");
    }
    if (string.IsNullOrWhiteSpace(password))
    {
        passwordError = "Password cannot be empty.";
        System.Diagnostics.Debug.WriteLine("Registration failed: Empty password");
    }

    if (usernameError == null && username.Length < 3)
    {
        usernameError = "Username must be at least 3 characters long.";
        System.Diagnostics.Debug.WriteLine("Registration failed: Username too short");
    }
    if (passwordError == null && password.Length < 8)
    {
        passwordError = "Password must be at least 8 characters long.";
        System.Diagnostics.Debug.WriteLine("Registration failed: Password too short");
    }

    if (usernameError == null && username.Contains(" "))
    {
        usernameError = "Username cannot contain spaces.";
        System.Diagnostics.Debug.WriteLine("Registration failed: Username contains spaces");
    }
    if (passwordError == null && password.Contains(" "))
    {
        passwordError = "Password cannot contain spaces.";
        System.Diagnostics.Debug.WriteLine("Registration failed: Password contains spaces");
    }

    if (usernameError == null && AuthService.UsersExist(username))
    {
        usernameError = "Username already exists.";
        System.Diagnostics.Debug.WriteLine($"Registration failed: Username {username} already exists");
    }

    if (usernameError != null)
    {
        UsernameError.Text = usernameError;
        UsernameError.Visibility = Visibility.Visible;
    }
    if (passwordError != null)
    {
        PasswordError.Text = passwordError;
        PasswordError.Visibility = Visibility.Visible;
    }

    if (usernameError != null || passwordError != null)
    {
        return;
    }

    if (AuthService.Register(username, password, name, out User newUser)) // Передаємо ім’я
    {
        System.Diagnostics.Debug.WriteLine($"Registration successful: User {username}, UserId: {newUser.UserId}, Name: {newUser.Name}");
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

            ThemeManager.ApplyTheme(user.Theme);
            System.Diagnostics.Debug.WriteLine($"Applied settings for new user: Theme={user.Theme}");

            var contactService = new ContactService(user.ContactFolderCode);
            MainView mainView = new MainView(contactService);
            mainView.Show();
            Window.GetWindow(this).Close();
        }
        else
        {
            PasswordError.Text = "Failed to log in after registration.";
            PasswordError.Visibility = Visibility.Visible;
            System.Diagnostics.Debug.WriteLine("Failed to log in after registration");
        }
    }
    else
    {
        PasswordError.Text = "Registration failed. Please try again.";
        PasswordError.Visibility = Visibility.Visible;
        System.Diagnostics.Debug.WriteLine("Registration failed: Unknown error");
    }
}

        private void LoginLink_Click(object sender, RoutedEventArgs e)
        {
            NavigateToLogin?.Invoke();
        }
    }
}