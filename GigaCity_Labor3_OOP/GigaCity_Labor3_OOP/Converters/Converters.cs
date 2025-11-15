using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using GigaCity_Labor3_OOP.Models;

namespace GigaCity_Labor3_OOP.Converters
{
    public class TerrainTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TerrainType terrainType)
            {
                return terrainType.ToString();
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str && Enum.TryParse<TerrainType>(str, out var terrainType))
            {
                return terrainType;
            }
            return TerrainType.Meadows;
        }
    }

    public class ResourceTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ResourceType resourceType)
            {
                return resourceType.ToString();
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str && Enum.TryParse<ResourceType>(str, out var resourceType))
            {
                return resourceType;
            }
            return ResourceType.None;
        }
    }

    public class ColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Color color)
            {
                return new SolidColorBrush(color);
            }
            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush brush)
            {
                return brush.Color;
            }
            return Colors.Transparent;
        }
    }
}