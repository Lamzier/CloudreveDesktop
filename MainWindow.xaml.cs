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
        // 已经登陆了
        InitUserInfoStorage();
    }

    private async void InitUserInfoStorage()
    {
        var userStorage = await UserApi.GetUserStorage();
        var code = (int)userStorage["code"]!;
        var msg = (string)userStorage["msg"]!;
        if (code != 0) MessageBox.Show(code + "：" + msg, "错误"); //其他异常
        var data = userStorage["data"];
        if (data == null) return;
        long free = 0;
        long total = 0;
        long used = 0;
        if (data["free"] != null) free = (long)data["free"]!;
        if (data["total"] != null) total = (long)data["total"]!;
        if (data["used"] != null) used = (long)data["used"]!;
        var totalT = total / 1024 / 1024 / 1024 / 1024;
        var usedG = used / 1024 / 1024 / 1024;
        var freeG = free / 1024 / 1024 / 1024;
        var totalTStr = totalT.ToString("F1") + " TB";
        var usedGStr = usedG.ToString("F1") + " GB";
        var freeGStr = freeG.ToString("F1") + " GB";
        //给UI更新
        Console.WriteLine(totalTStr);
        StorageFree.Text = "剩余：" + freeGStr;
        StorageTotal.Text = totalTStr;
        StorageUsed.Text = usedGStr;
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