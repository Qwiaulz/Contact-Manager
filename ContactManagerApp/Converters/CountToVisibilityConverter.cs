using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ContactManagerApp.Converters
{
    public class CountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count && parameter is string param)
            {
                var parts = param.Split(',');
                if (parts.Length != 3)
                    return Visibility.Collapsed;

                int threshold = int.Parse(parts[0]);
                Visibility whenEqual = (Visibility)Enum.Parse(typeof(Visibility), parts[1]);
                Visibility whenNotEqual = (Visibility)Enum.Parse(typeof(Visibility), parts[2]);

                return count == threshold ? whenEqual : whenNotEqual;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}