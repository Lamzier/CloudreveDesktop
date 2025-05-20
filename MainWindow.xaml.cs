using System.Windows;
using System.Windows.Input;

namespace CloudreveDesktop;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();
    }

    // 窗口拖动
    private void DragWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left) DragMove();
    }

    // 最小化 
    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    // 最大化/还原
    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        if (WindowState == WindowState.Maximized)
        {
            WindowState = WindowState.Normal;
            MaximizeRestoreButton.Content = "\xE922"; // 最大化图标
        }
        else
        {
            WindowState = WindowState.Maximized;
            MaximizeRestoreButton.Content = "\xE923"; // 还原图标
        }
    }

    // 关闭
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}