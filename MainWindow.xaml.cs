using System.Windows;
using CloudreveDesktop.CloudreveApi;

namespace CloudreveDesktop;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
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
            new LoginWindow().ShowDialog(); // 没登录 阻塞显示
            return;
        }

        if (code != 0) MessageBox.Show(code + "：" + msg, "错误"); //其他异常
        // 已经登陆无需操作，可能需要在这里拿点信息
    }
}