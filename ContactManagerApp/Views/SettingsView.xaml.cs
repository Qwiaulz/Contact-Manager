using ContactManagerApp.Services;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using ContactManagerApp.Models;

namespace ContactManagerApp.Views
{
    public partial class SettingsView : Page
    {
        private readonly NavigationService _navigationService;
        private readonly SettingService _settingService;

        public SettingsView(NavigationService navigationService)
        {
            InitializeComponent();
            _navigationService = navigationService;
            _settingService = new SettingService();

            LoadLanguagePreference();
            LoadThemePreference();

            LocalizationManager.LanguageChanged += OnLanguageChanged;
        }

        private void LoadLanguagePreference()
        {
            string currentLanguage = LocalizationManager.CurrentLanguage;
            foreach (ComboBoxItem item in LanguageComboBox.Items)
            {
                if (item.Tag.ToString() == currentLanguage)
                {
                    LanguageComboBox.SelectedItem = item;
                    break;
                }
            }
        }

        private void LoadThemePreference()
        {
            string currentTheme = ThemeManager.CurrentTheme ?? "Light";
            foreach (ComboBoxItem item in ThemeComboBox.Items)
            {
                if (item.Tag.ToString() == currentTheme)
                {
                    ThemeComboBox.SelectedItem = item;
                    break;
                }
            }
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LanguageComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string language = selectedItem.Tag.ToString();
                LocalizationManager.CurrentLanguage = language;

                string userId = _settingService.GetSetting("CurrentUserId") ?? App.CurrentUser?.UserId;
                if (!string.IsNullOrEmpty(userId))
                {
                    AuthService.UpdateUserSettings(userId, language, ThemeManager.CurrentTheme);
                    System.Diagnostics.Debug.WriteLine($"Saved language setting: Language={language}, UserId={userId}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Failed to save language setting: CurrentUserId is null");
                }

                UpdateUI();
            }
        }

        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ThemeComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string theme = selectedItem.Tag.ToString();
                ThemeManager.ApplyTheme(theme);

                string userId = _settingService.GetSetting("CurrentUserId") ?? App.CurrentUser?.UserId;
                if (!string.IsNullOrEmpty(userId))
                {
                    AuthService.UpdateUserSettings(userId, LocalizationManager.CurrentLanguage, theme);
                    System.Diagnostics.Debug.WriteLine($"Saved theme setting: Theme={theme}, UserId={userId}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Failed to save theme setting: CurrentUserId is null");
                }
            }
        }

        private void ChangePassword_Click(object sender, RoutedEventArgs e)
        {
            string oldPassword = OldPasswordBox.Password;
            string newPassword = NewPasswordBox.Password;
            string confirmNewPassword = ConfirmNewPasswordBox.Password;

            if (string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmNewPassword))
            {
                var dialog = new CustomConfirmationDialog
                {
                    Title = LocalizationManager.GetString("Error"),
                    Message = LocalizationManager.GetString("AllFieldsRequired"),
                    ConfirmButtonText = LocalizationManager.GetString("OK"),
                    CancelButtonText = "" // Приховуємо кнопку "Скасувати"
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
                return;
            }

            if (newPassword.Length < 8)
            {
                var dialog = new CustomConfirmationDialog
                {
                    Title = LocalizationManager.GetString("Error"),
                    Message = LocalizationManager.GetString("PasswordMinLength"),
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
                return;
            }

            if (newPassword != confirmNewPassword)
            {
                var dialog = new CustomConfirmationDialog
                {
                    Title = LocalizationManager.GetString("Error"),
                    Message = LocalizationManager.GetString("PasswordsDoNotMatch"),
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
                return;
            }

            string userId = _settingService.GetSetting("CurrentUserId") ?? App.CurrentUser?.UserId;
            if (string.IsNullOrEmpty(userId) && App.CurrentUser != null)
            {
                userId = App.CurrentUser.UserId;
                System.Diagnostics.Debug.WriteLine($"Using App.CurrentUser.UserId as fallback for password change: {userId}");
            }

            if (string.IsNullOrEmpty(userId))
            {
                System.Diagnostics.Debug.WriteLine("Change password failed: No valid userId found");
                var dialog = new CustomConfirmationDialog
                {
                    Title = LocalizationManager.GetString("Error"),
                    Message = LocalizationManager.GetString("UserNotFound"),
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
                return;
            }

            bool passwordChanged = AuthService.ChangePassword(userId, oldPassword, newPassword);
            if (passwordChanged)
            {
                var dialog = new CustomConfirmationDialog
                {
                    Title = LocalizationManager.GetString("Success"),
                    Message = LocalizationManager.GetString("PasswordChangedSuccess"),
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

                dialog.DialogResult += (s, result) =>
                {
                    OldPasswordBox.Password = string.Empty;
                    NewPasswordBox.Password = string.Empty;
                    ConfirmNewPasswordBox.Password = string.Empty;
                };
                window.ShowDialog();
            }
            else
            {
                var dialog = new CustomConfirmationDialog
                {
                    Title = LocalizationManager.GetString("Error"),
                    Message = LocalizationManager.GetString("OldPasswordIncorrect"),
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

        private void DeleteAccount_Click(object sender, RoutedEventArgs e)
{
    var dialog = new CustomConfirmationDialog
    {
        Title = LocalizationManager.GetString("DeleteAccount"),
        Message = LocalizationManager.GetString("DeleteAccountConfirmation"),
        ConfirmButtonText = LocalizationManager.GetString("DeleteAccount"),
        CancelButtonText = LocalizationManager.GetString("Cancel")
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

    dialog.DialogResult += (s, result) =>
    {
        if (!result)
            return;

        string userId = _settingService.GetSetting("CurrentUserId") ?? App.CurrentUser?.UserId;
        System.Diagnostics.Debug.WriteLine($"Attempting to delete account with CurrentUserId: {userId}");

        if (string.IsNullOrEmpty(userId) && App.CurrentUser != null)
        {
            userId = App.CurrentUser.UserId;
            System.Diagnostics.Debug.WriteLine($"Using App.CurrentUser.UserId as fallback: {userId}");
        }

        if (string.IsNullOrEmpty(userId))
        {
            System.Diagnostics.Debug.WriteLine("Delete account failed: No valid userId found");
            var errorDialog = new CustomConfirmationDialog
            {
                Title = LocalizationManager.GetString("Error"),
                Message = LocalizationManager.GetString("UserNotFound"),
                ConfirmButtonText = LocalizationManager.GetString("OK"),
                CancelButtonText = ""
            };

            var errorWindow = new Window
            {
                AllowsTransparency = true,
                Content = errorDialog,
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize,
                Background = null
            };

            errorDialog.DialogResult += (s2, result2) => { errorWindow.Close(); };
            errorWindow.ShowDialog();
            return;
        }

        bool deleted = AuthService.DeleteAccount(userId);
        if (deleted)
        {
            System.Diagnostics.Debug.WriteLine($"Account deletion successful for UserId: {userId}");
            _settingService.SetSetting("CurrentUserId", null);
            _settingService.SetSetting("AuthToken", null);
            _settingService.SetCurrentUserIdLastUsed(null);
            _settingService.SetSessionExpiredShown(false);
            App.CurrentUser = null;

            // Закриваємо поточне вікно підтвердження перед переходом
            window.Close();

            // Перехід до AuthView
            var authView = new AuthView();
            authView.Show();
            Window.GetWindow(this)?.Close();

            var successDialog = new CustomConfirmationDialog
            {
                Title = LocalizationManager.GetString("Success"),
                Message = LocalizationManager.GetString("DeleteAccountSuccess"),
                ConfirmButtonText = LocalizationManager.GetString("OK"),
                CancelButtonText = ""
            };

            var successWindow = new Window
            {
                AllowsTransparency = true,
                Content = successDialog,
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize,
                Background = null
            };

            successDialog.DialogResult += (s2, result2) => { successWindow.Close(); };
            successWindow.ShowDialog();
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"Delete account failed for UserId: {userId}");
            var errorDialog = new CustomConfirmationDialog
            {
                Title = LocalizationManager.GetString("Error"),
                Message = LocalizationManager.GetString("DeleteAccountFailed"),
                ConfirmButtonText = LocalizationManager.GetString("OK"),
                CancelButtonText = ""
            };

            var errorWindow = new Window
            {
                AllowsTransparency = true,
                Content = errorDialog,
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize,
                Background = null
            };

            errorDialog.DialogResult += (s2, result2) => { errorWindow.Close(); };
            errorWindow.ShowDialog();
        }
    };

    window.ShowDialog();
}

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            UpdateUI();
        }

        private void UpdateUI()
        {
            DataContext = null;
            DataContext = this;
            UpdateBindings(this);

            if (Window.GetWindow(this) is MainView mainView)
            {
                mainView.DataContext = null;
                mainView.DataContext = mainView;
                UpdateBindings(mainView);
            }
        }

        private void UpdateBindings(DependencyObject element)
        {
            foreach (object child in LogicalTreeHelper.GetChildren(element))
            {
                if (child is DependencyObject depObj)
                {
                    if (depObj is TextBlock textBlock)
                    {
                        var bindingExpression = textBlock.GetBindingExpression(TextBlock.TextProperty);
                        bindingExpression?.UpdateTarget();
                    }
                    else if (depObj is ContentControl contentControl && contentControl.Content is TextBlock contentTextBlock)
                    {
                        var bindingExpression = contentTextBlock.GetBindingExpression(TextBlock.TextProperty);
                        bindingExpression?.UpdateTarget();
                    }
                    else if (depObj is Button button)
                    {
                        var bindingExpression = button.GetBindingExpression(Button.ContentProperty);
                        bindingExpression?.UpdateTarget();
                    }

                    UpdateBindings(depObj);
                }
            }
        }

        ~SettingsView()
        {
            LocalizationManager.LanguageChanged -= OnLanguageChanged;
        }
    }
}