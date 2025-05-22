using ContactManagerApp.Services;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using ContactManagerApp.Models;
using Microsoft.Win32;

namespace ContactManagerApp.Views
{
    public partial class SettingsView : Page
    {
        private readonly NavigationService _navigationService;
        private readonly SettingService _settingService;
        private User _currentUser;
        private User _initialUserState; // Початковий стан для порівняння
        private readonly string _photosDirectory;

        public User CurrentUser
        {
            get => _currentUser;
            set
            {
                _currentUser = value;
                OnPropertyChanged(nameof(CurrentUser));
            }
        }

        public ICommand ChoosePhotoCommand { get; }
        public ICommand RemovePhotoCommand { get; }
        public ICommand CancelContextMenuCommand { get; }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        public SettingsView(NavigationService navigationService)
        {
            InitializeComponent();
            _navigationService = navigationService;
            _settingService = new SettingService();

            string projectRoot = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;
            _photosDirectory = Path.Combine(projectRoot, "Data", "Photos");
            if (!Directory.Exists(_photosDirectory))
            {
                Directory.CreateDirectory(_photosDirectory);
            }

            CurrentUser = App.CurrentUser;
            if (CurrentUser == null)
            {
                string userId = _settingService.GetSetting("CurrentUserId");
                string token = _settingService.GetSetting("AuthToken");
                CurrentUser = AuthService.ValidateToken(token);
                if (CurrentUser == null)
                {
                    System.Diagnostics.Debug.WriteLine("No valid user found, redirecting to AuthView");
                    var authView = new AuthView();
                    authView.Show();
                    Window.GetWindow(this)?.Close();
                    return;
                }
            }

            // Зберігаємо початковий стан користувача
            _initialUserState = CurrentUser.Clone();

            ChoosePhotoCommand = new RelayCommand<object>(ChoosePhoto);
            RemovePhotoCommand = new RelayCommand<object>(RemovePhoto);
            CancelContextMenuCommand = new RelayCommand<object>(CancelContextMenu);

            DataContext = this;

            if (PhotoActionIcon != null && PhotoActionIcon.ContextMenu != null)
            {
                PhotoActionIcon.ContextMenu.DataContext = this;
            }

            LoadLanguagePreference();
            LoadThemePreference();

            LocalizationManager.LanguageChanged += OnLanguageChanged;

            // Підписуємося на зміни властивостей CurrentUser
            CurrentUser.PropertyChanged += CurrentUser_PropertyChanged;

            // Перевірка стану фото при ініціалізації
            CheckPhotoExistence();
        }

        private void CurrentUser_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(HasUnsavedChanges));
            if (e.PropertyName == nameof(CurrentUser.PhotoPath))
            {
                CheckPhotoExistence();
            }
        }

        public bool HasUnsavedChanges
        {
            get
            {
                if (_initialUserState == null || CurrentUser == null)
                    return false;

                return CurrentUser.Name != _initialUserState.Name ||
                       CurrentUser.PhoneNumber != _initialUserState.PhoneNumber ||
                       CurrentUser.Email != _initialUserState.Email ||
                       CurrentUser.PhotoPath != _initialUserState.PhotoPath;
            }
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
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

                UpdateUI();
            }
        }

        private void ChoosePhoto(object parameter)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg|All files (*.*)|*.*",
                Title = "Select a profile photo"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string sourceFilePath = openFileDialog.FileName;
                string extension = Path.GetExtension(sourceFilePath).ToLower();
                if (extension != ".png" && extension != ".jpeg" && extension != ".jpg")
                {
                    var dialog = new CustomConfirmationDialog
                    {
                        Title = LocalizationManager.GetString("Error"),
                        Message = LocalizationManager.GetString("InvalidImageFormat"),
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

                string timestamp = DateTime.Now.Ticks.ToString();
                string newFileName = $"user_{CurrentUser.UserId}_{timestamp}{extension}";
                string destinationFilePath = Path.Combine(_photosDirectory, newFileName);

                try
                {
                    if (!string.IsNullOrEmpty(CurrentUser.PhotoPath) && File.Exists(CurrentUser.PhotoPath))
                    {
                        File.Delete(CurrentUser.PhotoPath);
                    }

                    File.Copy(sourceFilePath, destinationFilePath, true);
                    CurrentUser.PhotoPath = destinationFilePath;
                    CurrentUser.IsPhotoDefault = false; // Оновлюємо статус фото
                    OnPropertyChanged(nameof(CurrentUser));

                    string userId = _settingService.GetSetting("CurrentUserId") ?? App.CurrentUser?.UserId;
                    if (!string.IsNullOrEmpty(userId))
                    {
                        AuthService.UpdateUserProfile(userId, CurrentUser.Name, CurrentUser.PhoneNumber, CurrentUser.Email, CurrentUser.PhotoPath);
                        App.CurrentUser = CurrentUser;
                        _initialUserState = CurrentUser.Clone(); // Оновлюємо початковий стан після збереження
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error saving photo: {ex.Message}");
                    var dialog = new CustomConfirmationDialog
                    {
                        Title = LocalizationManager.GetString("Error"),
                        Message = LocalizationManager.GetString("PhotoSaveFailed"),
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

            if (PhotoActionIcon.ContextMenu != null)
            {
                PhotoActionIcon.ContextMenu.IsOpen = false;
            }
        }

        private void RemovePhoto(object parameter)
        {
            if (!string.IsNullOrEmpty(CurrentUser.PhotoPath))
            {
                string fullPath = CurrentUser.PhotoPath;
                if (File.Exists(fullPath))
                {
                    try
                    {
                        File.Delete(fullPath);
                    }
                    catch (Exception ex)
                    {
                        var dialog = new CustomConfirmationDialog
                        {
                            Title = LocalizationManager.GetString("Error"),
                            Message = LocalizationManager.GetString("PhotoDeleteError", ex.Message),
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
                }
                CurrentUser.PhotoPath = null;
                CurrentUser.IsPhotoDefault = true; // Позначаємо як дефолтне фото
                CurrentUser.OnPropertyChanged(nameof(CurrentUser.PhotoPath));
                CurrentUser.OnPropertyChanged(nameof(CurrentUser.Initials));
                CurrentUser.OnPropertyChanged(nameof(CurrentUser.IsPhotoDefault));

                typeof(User).GetMethod("UpdatePhotoIfNameIsEmptyOrDigits", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(CurrentUser, null);

                string userId = _settingService.GetSetting("CurrentUserId") ?? App.CurrentUser?.UserId;
                if (!string.IsNullOrEmpty(userId))
                {
                    AuthService.UpdateUserProfile(userId, CurrentUser.Name, CurrentUser.PhoneNumber, CurrentUser.Email, CurrentUser.PhotoPath);
                    App.CurrentUser = CurrentUser;
                    _initialUserState = CurrentUser.Clone(); // Оновлюємо початковий стан після збереження
                }
            }

            if (PhotoActionIcon.ContextMenu != null)
            {
                PhotoActionIcon.ContextMenu.IsOpen = false;
            }
        }

        private void CancelContextMenu(object parameter)
        {
            if (PhotoActionIcon.ContextMenu != null)
            {
                PhotoActionIcon.ContextMenu.IsOpen = false;
            }
        }

        private void PhotoActionIcon_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Перевіряємо стан фото перед дією
            CheckPhotoExistence();

            // Якщо фото відсутнє або є дефолтним, відкриваємо провідник
            if (string.IsNullOrEmpty(CurrentUser.PhotoPath) || CurrentUser.IsPhotoDefault)
            {
                ChoosePhotoCommand.Execute(null);
                e.Handled = true;
            }
            else
            {
                // Якщо фото не дефолтне, відкриваємо контекстне меню
                if (PhotoActionIcon != null && PhotoActionIcon.ContextMenu != null)
                {
                    PhotoActionIcon.ContextMenu.IsOpen = true;
                    e.Handled = true;
                }
            }
        }

        private void CheckPhotoExistence()
        {
            if (!string.IsNullOrEmpty(CurrentUser.PhotoPath) && !File.Exists(CurrentUser.PhotoPath))
            {
                CurrentUser.PhotoPath = null;
                CurrentUser.IsPhotoDefault = true;
                CurrentUser.OnPropertyChanged(nameof(CurrentUser.PhotoPath));
                CurrentUser.OnPropertyChanged(nameof(CurrentUser.Initials));
                CurrentUser.OnPropertyChanged(nameof(CurrentUser.IsPhotoDefault));

                string userId = _settingService.GetSetting("CurrentUserId") ?? App.CurrentUser?.UserId;
                if (!string.IsNullOrEmpty(userId))
                {
                    AuthService.UpdateUserProfile(userId, CurrentUser.Name, CurrentUser.PhoneNumber, CurrentUser.Email, CurrentUser.PhotoPath);
                    App.CurrentUser = CurrentUser;
                    _initialUserState = CurrentUser.Clone();
                }
            }
        }

        private void SaveProfile_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateData())
            {
                SaveProfileChanges(showSuccessDialog: true); // Показуємо діалог успіху при ручному збереженні
            }
        }

        public bool ValidateData()
        {
            string email = EmailTextBox.Text;
            if (!string.IsNullOrEmpty(email) && !email.Contains("@"))
            {
                var dialog = new CustomConfirmationDialog
                {
                    Title = LocalizationManager.GetString("Error"),
                    Message = LocalizationManager.GetString("InvalidEmailFormat"),
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
                return false;
            }
            return true;
        }

        public void SaveProfileChanges(bool showSuccessDialog)
        {
            string userId = _settingService.GetSetting("CurrentUserId") ?? App.CurrentUser?.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                System.Diagnostics.Debug.WriteLine("Save profile failed: No valid userId found");
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

            string email = EmailTextBox.Text;
            if (!string.IsNullOrEmpty(email) && !email.Contains("@"))
            {
                var dialog = new CustomConfirmationDialog
                {
                    Title = LocalizationManager.GetString("Error"),
                    Message = LocalizationManager.GetString("InvalidEmailFormat"),
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

            bool profileUpdated = AuthService.UpdateUserProfile(userId, NameTextBox.Text, PhoneTextBox.Text, EmailTextBox.Text, CurrentUser.PhotoPath);
            if (profileUpdated)
            {
                App.CurrentUser = CurrentUser;
                _initialUserState = CurrentUser.Clone(); // Оновлюємо початковий стан після збереження

                // Показуємо діалог успіху, лише якщо showSuccessDialog == true
                if (showSuccessDialog)
                {
                    var dialog = new CustomConfirmationDialog
                    {
                        Title = LocalizationManager.GetString("Success"),
                        Message = LocalizationManager.GetString("ProfileUpdatedSuccess"),
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
            else
            {
                var dialog = new CustomConfirmationDialog
                {
                    Title = LocalizationManager.GetString("Error"),
                    Message = LocalizationManager.GetString("ProfileUpdateFailed"),
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

                    window.Close();

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

            if (PhotoActionIcon?.ContextMenu != null)
            {
                foreach (var item in PhotoActionIcon.ContextMenu.Items)
                {
                    if (item is MenuItem menuItem)
                    {
                        var headerBinding = menuItem.GetBindingExpression(MenuItem.HeaderProperty);
                        headerBinding?.UpdateTarget();

                        var backgroundBinding = menuItem.GetBindingExpression(MenuItem.BackgroundProperty);
                        backgroundBinding?.UpdateTarget();

                        var foregroundBinding = menuItem.GetBindingExpression(MenuItem.ForegroundProperty);
                        foregroundBinding?.UpdateTarget();

                        menuItem.Style = null;
                        menuItem.Style = (Style)FindResource("ContextMenuItemStyle");

                        UpdateVisualChildren(menuItem);
                    }
                }

                var contextMenuBackgroundBinding = PhotoActionIcon.ContextMenu.GetBindingExpression(ContextMenu.BackgroundProperty);
                contextMenuBackgroundBinding?.UpdateTarget();

                if (PhotoActionIcon.ContextMenu.IsOpen)
                {
                    PhotoActionIcon.ContextMenu.IsOpen = false;
                    PhotoActionIcon.ContextMenu.IsOpen = true;
                }
            }
        }

        private void UpdateVisualChildren(DependencyObject parent)
        {
            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is FrameworkElement element)
                {
                    var backgroundBinding = element.GetBindingExpression(BackgroundProperty);
                    backgroundBinding?.UpdateTarget();

                    var foregroundBinding = element.GetBindingExpression(ForegroundProperty);
                    foregroundBinding?.UpdateTarget();

                    UpdateVisualChildren(child);
                }
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
                    else if (depObj is ComboBox comboBox)
                    {
                        foreach (ComboBoxItem item in comboBox.Items)
                        {
                            if (item.Content is TextBlock comboTextBlock)
                            {
                                var bindingExpression = comboTextBlock.GetBindingExpression(TextBlock.TextProperty);
                                bindingExpression?.UpdateTarget();
                            }
                        }
                    }

                    UpdateBindings(depObj);
                }
            }
        }

        private void PasswordBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            DependencyObject current = sender as DependencyObject;
            while (current != null && !(current is ScrollViewer))
            {
                current = VisualTreeHelper.GetParent(current);
            }

            if (current is ScrollViewer scrollViewer)
            {
                if (e.Delta > 0)
                {
                    scrollViewer.LineUp();
                }
                else
                {
                    scrollViewer.LineDown();
                }

                e.Handled = true;
            }
        }

        public void DiscardChanges()
        {
            CurrentUser.RestoreFrom(_initialUserState);
        }

        ~SettingsView()
        {
            LocalizationManager.LanguageChanged -= OnLanguageChanged;
            if (CurrentUser != null)
            {
                CurrentUser.PropertyChanged -= CurrentUser_PropertyChanged;
            }
        }
    }
}