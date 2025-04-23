using System;
using System.Globalization;
using System.Windows.Data;
using ContactManagerApp.Services;

namespace ContactManagerApp.Converters
{
    public class LocalizedDateConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2 || values[0] == null || values[1] == null)
                return string.Empty;

            if (values[0] is DateTime date && values[1] is string formatString)
            {
                // Отримуємо день і рік із дати
                string day = date.ToString("dd");
                string year = date.ToString("yyyy");

                // Отримуємо місяць і локалізуємо його
                string monthKey = date.ToString("MMMM", CultureInfo.InvariantCulture); // Наприклад, "January"
                string localizedMonth = LocalizationManager.GetString(monthKey);

                // Формуємо локалізований рядок дати
                string localizedDate = $"{day} {localizedMonth} {year}";

                // Форматуємо кінцевий рядок із локалізованим шаблоном
                return string.Format(formatString, localizedDate);
            }

            return string.Empty;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}