using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace CitySimulation.Views
{
    public class BooleanToColorConverter : IValueConverter
    {
        public static BooleanToColorConverter Instance { get; } = new BooleanToColorConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Brushes.Green : Brushes.Red;
            }
            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
