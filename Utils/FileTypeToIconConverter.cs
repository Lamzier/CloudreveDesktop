using System.Globalization;
using System.Windows.Data;

namespace CloudreveDesktop.utils;

public class FileTypeToIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var fileType = value?.ToString()?.ToLower() ?? "";

        return fileType switch
        {
            "dir" => "/Resources/folder.png", // 文件夹
            "pdf" => "/Resources/pdf.png", // PDF文件
            "jpg" or "png" => "/Resources/image.png", // 图片文件
            "zip" or "rar" => "/Resources/archive.png", // 压缩文件
            _ => "/Resources/file.png" // 默认文件图标
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}