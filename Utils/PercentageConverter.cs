using System.Globalization;
using System.Windows.Data;

namespace CloudreveDesktop.utils;

public class PercentageConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double actualWidth &&
            double.TryParse(parameter?.ToString(), out var percentage))
            // 减去滚动条宽度补偿值
            return actualWidth * percentage;
        return 0;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}