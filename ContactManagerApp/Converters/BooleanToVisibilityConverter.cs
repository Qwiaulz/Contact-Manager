using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ContactManagerApp.Converters
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter == null)
            {
                if (value is bool boolValue)
                {
                    return boolValue ? Visibility.Visible : Visibility.Collapsed;
                }
                return Visibility.Collapsed;
            }

            string[] parameters = parameter.ToString().Split(',');
            if (parameters.Length != 3)
            {
                return Visibility.Collapsed;
            }

            string nullValue = parameters[0];
            string trueValue = parameters[1];
            string falseValue = parameters[2];
            
            bool isTrue;
            if (value == null || (value is string str && string.IsNullOrEmpty(str)))
            {
                isTrue = nullValue == "Visible";
            }
            else if (value is bool boolValue)
            {
                isTrue = boolValue ? trueValue == "Visible" : falseValue == "Visible";
            }
            else
            {
                // Для рядків (наприклад, шлях до фото), вважаємо, що це "не null", тому використовуємо falseValue
                isTrue = falseValue == "Visible";
            }

            var result = isTrue ? Visibility.Visible : Visibility.Collapsed;
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Visible;
            }
            return false;
        }
    }
}