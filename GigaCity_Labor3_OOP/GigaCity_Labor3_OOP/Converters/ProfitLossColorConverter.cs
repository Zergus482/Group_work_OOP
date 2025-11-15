using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace GigaCity_Labor3_OOP.Converters
{
    public class ProfitLossColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string profitLossText)
            {
                // Проверяем, положительная ли прибыль (содержит + или просто число без минуса в начале)
                if (profitLossText.StartsWith("+") ||
                    (profitLossText.StartsWith("$") && !profitLossText.Contains("-") && !profitLossText.Contains("(")))
                {
                    return Brushes.LightGreen;
                }
                else if (profitLossText.Contains("-") || profitLossText.Contains("("))
                {
                    return Brushes.LightCoral;
                }
            }
            return Brushes.White;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}