using System.Diagnostics;
using System.Windows;

namespace CloudreveDesktop.utils;

public class AppUtil
{
    // 重启软件
    public static void Restart()
    {
        try
        {
            var appPath = Environment.ProcessPath;

            if (string.IsNullOrEmpty(appPath))
                throw new InvalidOperationException("无法获取当前应用程序路径");

            var startInfo = new ProcessStartInfo(appPath)
            {
                UseShellExecute = true
            };

            Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"重启失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        Application.Current.Shutdown();
    }
}