using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ContactManagerApp.Converters
{
    public class PhotoVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var contact = value as ContactManagerApp.Models.Contact;
            if (contact == null)
            {
                return Visibility.Visible; // Показуємо кружечок, якщо контакт недоступний
            }

            // Показуємо AddPhotoCircle, якщо фото відсутнє (null або порожнє) або є дефолтним
            bool isPhotoEmpty = string.IsNullOrEmpty(contact.Photo);
            bool isPhotoDefault = contact.IsPhotoDefault;
            return (isPhotoEmpty || isPhotoDefault) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}