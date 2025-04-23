using System;
using System.Globalization;
using System.Windows.Data;
using ContactManagerApp.Services;

namespace ContactManagerApp.Converters
{
    public class LocalizationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is string key)
            {
                var result = LocalizationManager.GetString(key);
                return result;
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}