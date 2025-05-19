using System;
using System.Windows;
using ContactManagerApp.Views;

namespace ContactManagerApp.Services
{
    public static class ThemeManager
    {
        private static string _currentTheme = "Light"; // Тема за замовчуванням
        private static readonly string[] AvailableThemes = { "Light", "Dark", "HighContrast" };
        private static ResourceDictionary _themeDictionary; // Зберігаємо словник теми окремо

        public static string CurrentTheme => _currentTheme;

        public static void ApplyTheme(string themeName)
        {
            if (!Array.Exists(AvailableThemes, t => t.Equals(themeName, StringComparison.OrdinalIgnoreCase)))
                themeName = "Light";

            try
            {
                var appResources = Application.Current.Resources;

                // Видаляємо попередній словник теми, якщо він існує
                if (_themeDictionary != null && appResources.MergedDictionaries.Contains(_themeDictionary))
                {
                    appResources.MergedDictionaries.Remove(_themeDictionary);
                }

                // Завантажуємо новий словник теми
                var themeUri = new Uri($"Themes/{themeName}Theme.xaml", UriKind.Relative);
                _themeDictionary = new ResourceDictionary { Source = themeUri };
                appResources.MergedDictionaries.Add(_themeDictionary);

                _currentTheme = themeName;
            }
            catch (Exception ex)
            {
                var dialog = new CustomConfirmationDialog
                {
                    Title = LocalizationManager.GetString("Error"),
                    Message = LocalizationManager.GetString("ThemeApplicationError", ex.Message),
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
}