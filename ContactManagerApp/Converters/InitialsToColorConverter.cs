using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace ContactManagerApp.Converters
{
    public class InitialsToColorConverter : IValueConverter
    {
        private static readonly List<string> Colors = new List<string>
        {
            "#3F51B5", "#E91E63", "#4CAF50", "#FF5722", "#2196F3",
            "#9C27B0", "#FF9800", "#009688", "#607D8B", "#795548",
            "#F44336", "#673AB7", "#00BCD4", "#8BC34A", "#FFEB3B",
            "#03A9F4", "#CDDC39", "#FFCA28", "#9E9E9E", "#D81B60"
        };

        // Метод для стабільного хешування рядка (FNV-1a)
        private static uint Fnv1aHash(string input)
        {
            const uint fnvPrime = 16777619u;
            const uint fnvOffset = 2166136261u;

            uint hash = fnvOffset;
            byte[] bytes = Encoding.UTF8.GetBytes(input);

            foreach (byte b in bytes)
            {
                hash ^= b;
                hash *= fnvPrime;
            }

            return hash;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string initials && !string.IsNullOrEmpty(initials))
            {
                // Використовуємо стабільний хеш замість GetHashCode
                uint hash = Fnv1aHash(initials);
                int index = (int)(hash % (uint)Colors.Count);
                return Colors[index];
            }
            return "#D3D3D3";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}