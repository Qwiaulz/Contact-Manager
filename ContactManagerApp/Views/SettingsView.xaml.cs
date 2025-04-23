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
                MessageBox.Show(LocalizationManager.GetString("AllFieldsRequired"), LocalizationManager.GetString("Error"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (newPassword.Length < 8)
            {
                MessageBox.Show(LocalizationManager.GetString("PasswordMinLength"), LocalizationManager.GetString("Error"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (newPassword != confirmNewPassword)
            {
                MessageBox.Show(LocalizationManager.GetString("PasswordsDoNotMatch"), LocalizationManager.GetString("Error"), MessageBoxButton.OK, MessageBoxImage.Warning);
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
                MessageBox.Show(LocalizationManager.GetString("UserNotFound") ?? "Користувача не знайдено.", LocalizationManager.GetString("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            bool passwordChanged = AuthService.ChangePassword(userId, oldPassword, newPassword);
            if (passwordChanged)
            {
                MessageBox.Show(LocalizationManager.GetString("PasswordChangedSuccess"), "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                OldPasswordBox.Password = string.Empty;
                NewPasswordBox.Password = string.Empty;
                ConfirmNewPasswordBox.Password = string.Empty;
            }
            else
            {
                MessageBox.Show(LocalizationManager.GetString("OldPasswordIncorrect"), LocalizationManager.GetString("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteAccount_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(
                LocalizationManager.GetString("DeleteAccountConfirmation"),
                LocalizationManager.GetString("DeleteAccount"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
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
                MessageBox.Show(LocalizationManager.GetString("UserNotFound") ?? "Користувача не знайдено.", LocalizationManager.GetString("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
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

                LoginView loginView = new LoginView();
                loginView.Show();
                Window.GetWindow(this)?.Close();
                MessageBox.Show(LocalizationManager.GetString("DeleteAccountSuccess") ?? "Обліковий запис успішно видалено.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Delete account failed for UserId: {userId}");
                MessageBox.Show(LocalizationManager.GetString("DeleteAccountFailed") ?? "Не вдалося видалити обліковий запис. Спробуйте ще раз.", LocalizationManager.GetString("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
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