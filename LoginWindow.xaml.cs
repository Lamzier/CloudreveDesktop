using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Windows;

namespace CloudreveDesktop;

public partial class LoginWindow
{
    private bool _isLogin;

    public LoginWindow()
    {
        InitializeComponent();
        Init();
    }

    private void Init()
    {
        DomainName.Text = "服务器地址：" + App.DomainName;
        ServerName.Text = "登陆 " + App.ServerName;
    }

    // 登陆
    private async void Login_Click(object sender, RoutedEventArgs e)
    {
        var email = EmailInput.Text;
        var password = PasswordInput.Password;
        var pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        var regex = new Regex(pattern);
        var isValidEmail = regex.IsMatch(email);
        if (!isValidEmail)
        {
            MessageBox.Show("邮箱地址不合法，请重新输入", "提示");
            return;
        }

        if (password.Length < 3)
        {
            MessageBox.Show("密码错误！", "提示");
            return;
        }

        LoginButton.IsEnabled = false;
        //都正确，执行登陆操作
        await Login(email, password, "");
        LoginButton.IsEnabled = true;
    }

    private async Task Login(string username, string password, string captchaCode)
    {
        var loginData = new
        {
            Password = password,
            userName = username,
            captchaCode
        };
        var loginJson = JsonSerializer.Serialize(loginData);
        var jsonContent = new StringContent(loginJson, Encoding.UTF8, "application/json");
        var response = await App.HttpClient.PostAsync(App.ServerUrl + "api/v3/user/session", jsonContent);
        var responseString = await response.Content.ReadAsStringAsync();
        var json = JsonNode.Parse(responseString)!;
        var code = (int)json["code"]!;
        var msg = (string)json["msg"]!;
        if (code != 0)
        {
            MessageBox.Show(msg, "错误");
            return;
        }

        // 登陆成功
        var cookies = response.Headers.GetValues("Set-Cookie").ToList();
        App.Cookies.Clear(); //清除cookies
        App.Cookies.AddRange(cookies); // 添加cookies
        App.UserName = username;
        App.Password = password;
        App.UpdateUser(); //更新数据到本地文件
        _isLogin = true;
        // 重新渲染主窗体UI
        App.IsLoggedIn = true;
        MainWindow.Instance.Rendering();
        Close();
    }

    private void DomainName_Click(object sender, RoutedEventArgs e)
    {
        var proc = new Process();
        proc.StartInfo.FileName = App.ServerUrl;
        proc.StartInfo.UseShellExecute = true;
        proc.Start();
    }

    private void ForgetPassword_Click(object sender, RoutedEventArgs e)
    {
        var proc = new Process();
        proc.StartInfo.FileName = App.ServerUrl;
        proc.StartInfo.UseShellExecute = true;
        proc.Start();
    }


    protected override void OnClosing(CancelEventArgs e)
    {
        if (_isLogin)
        {
            base.OnClosing(e);
            return;
        }

        var result = MessageBox.Show("不登录则无法正常使用，是否关闭？", "信息", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.No) e.Cancel = true; //阻止关闭
        else Application.Current.Shutdown(); //关闭整个程序
        base.OnClosing(e);
    }
}