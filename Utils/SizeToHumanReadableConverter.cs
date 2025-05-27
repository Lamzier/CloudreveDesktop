using System.Globalization;
using System.Windows.Data;

namespace CloudreveDesktop.utils;

public class SizeToHumanReadableConverter : IValueConverter
{
    private static readonly string[] Units = { "B", "KB", "MB", "GB", "TB" };

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null) return "0 B";

        long size;
        if (value is string strSize && long.TryParse(strSize, out size)) return FormatSize(size);
        if (value is long || value is int)
        {
            size = System.Convert.ToInt64(value);
            return FormatSize(size);
        }

        return "N/A";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    private string FormatSize(long bytes)
    {
        if (bytes == 0) return string.Empty;
        var unitIndex = 0;
        double size = bytes;

        while (size >= 1024 && unitIndex < Units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }

        return $"{size:0.##} {Units[unitIndex]}";
    }
}