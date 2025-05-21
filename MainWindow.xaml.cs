using System.Windows;
using System.Windows.Input;
using CloudreveDesktop.CloudreveApi;

namespace CloudreveDesktop;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly LoginWindow _loginWindow = new();

    public MainWindow()
    {
        InitializeComponent();
        CheckLogin(); //检查登陆
    }

    private async void CheckLogin()
    {
        var userSetting = await UserApi.GetUserSetting();
        var code = (int)userSetting["code"]!;
        var msg = (string)userSetting["msg"]!;
        if (code == 401)
        {
            _loginWindow.ShowDialog(); // 没登录 阻塞显示
            return;
        }

        if (code != 0)
        {
            MessageBox.Show(code + "：" + msg, "错误"); //其他异常
            return;
        }
        // 已经登陆无需操作，可能需要在这里拿点信息
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
}