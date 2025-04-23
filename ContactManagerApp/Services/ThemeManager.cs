using System;
using System.Windows;

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
                MessageBox.Show($"Помилка при застосуванні теми: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}