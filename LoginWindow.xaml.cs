using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CloudreveDesktop.CloudreveApi;
using CloudreveDesktop.utils;

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
        MainWindow.Instance.Rendering();
    }

    private async Task Login(string username, string password, string captchaCode)
    {
        var json = await UserApi.Login(username, password, captchaCode);
        var code = (int)json["code"]!;
        var msg = (string)json["msg"]!;
        if (code != 0)
        {
            MessageBox.Show(msg, "错误");
            return;
        }

        // 登陆成功
        App.UserName = username;
        App.Password = password;
        App.UpdateUser(); //更新数据到本地文件
        _isLogin = true;
        // 重新渲染主窗体UI
        App.IsLoggedIn = true;
        MainWindow.Instance.Show();
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

        Application.Current.Shutdown(); //关闭整个程序，因为没有登陆
        base.OnClosing(e);
    }

    private void ChangeServer_Click(object sender, RoutedEventArgs e)
    {
        var inputDialog = new Window
        {
            Title = "修改服务器配置",
            Width = 400,
            Height = 380,
            WindowStartupLocation = WindowStartupLocation.CenterScreen, // 强制屏幕居中
            ResizeMode = ResizeMode.NoResize,
            Icon = Application.Current.Resources["SettingsIcon"] as ImageSource // 使用资源图标
        };

        var mainStack = new StackPanel
        {
            Margin = new Thickness(20),
            VerticalAlignment = VerticalAlignment.Center
        };

        // 统一控件样式
        var labelStyle = new Style(typeof(Label))
        {
            Setters =
            {
                new Setter(FontSizeProperty, 14.0),
                new Setter(ForegroundProperty, Brushes.DimGray),
                new Setter(MarginProperty, new Thickness(0, 8, 0, 2))
            }
        };

        var textBoxStyle = new Style(typeof(TextBox))
        {
            Setters =
            {
                new Setter(FontSizeProperty, 14.0),
                new Setter(HeightProperty, 30.0),
                new Setter(PaddingProperty, new Thickness(5)),
                new Setter(BorderBrushProperty, Brushes.LightGray),
                new Setter(BorderThicknessProperty, new Thickness(1)),
                new Setter(VerticalContentAlignmentProperty, VerticalAlignment.Center)
            }
        };

        // 服务器地址
        var spUrl = new StackPanel();
        spUrl.Children.Add(new Label { Content = "服务器地址：", Style = labelStyle });
        var txtUrl = new TextBox
        {
            Text = App.ServerUrl,
            ToolTip = "支持http/https协议\n示例：http://127.0.0.1:5212/",
            Style = textBoxStyle
        };
        spUrl.Children.Add(txtUrl);

        // 服务器域名
        var spDomain = new StackPanel { Margin = new Thickness(0, 10, 0, 0) };
        spDomain.Children.Add(new Label { Content = "服务器域名：", Style = labelStyle });
        var txtDomain = new TextBox
        {
            Text = App.DomainName,
            ToolTip = "支持域名或IP地址\n示例：nas.lamzy.cn",
            Style = textBoxStyle
        };
        spDomain.Children.Add(txtDomain);

        // 服务器名称
        var spName = new StackPanel { Margin = new Thickness(0, 10, 0, 0) };
        spName.Children.Add(new Label { Content = "服务器名称：", Style = labelStyle });
        var txtName = new TextBox
        {
            Text = App.ServerName,
            ToolTip = "用于显示的友好名称\n示例：LamNas",
            Style = textBoxStyle
        };
        spName.Children.Add(txtName);

        // 按钮容器
        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 20, 0, 0)
        };

        // 带样式的按钮
        var btnStyle = new Style(typeof(Button))
        {
            Setters =
            {
                new Setter(WidthProperty, 90.0),
                new Setter(HeightProperty, 32.0),
                new Setter(FontSizeProperty, 13.0),
                new Setter(ForegroundProperty, Brushes.White),
                new Setter(PaddingProperty, new Thickness(15, 8, 15, 8)),
                new Setter(MarginProperty, new Thickness(10, 0, 0, 0)),
                new Setter(BorderThicknessProperty, new Thickness(0)),
                new Setter(BorderBrushProperty, Brushes.Transparent)
            }
        };

        var btnOk = new Button
        {
            Content = "保 存",
            Style = btnStyle,
            Background = new SolidColorBrush(Color.FromRgb(0, 120, 215))
        };

        var btnCancel = new Button
        {
            Content = "取 消",
            Style = btnStyle,
            Background = new SolidColorBrush(Color.FromRgb(150, 150, 150))
        };
        // 事件处理
        btnOk.Click += (s, args) =>
        {
            var url = txtUrl.Text.Trim();
            var domain = txtDomain.Text.Trim();
            var name = txtName.Text.Trim();
            // URL校验逻辑
            if (!url.EndsWith("/"))
            {
                MessageBox.Show("服务器地址必须以斜杠(/)结尾！",
                    "输入错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                txtUrl.Focus();
                txtUrl.SelectAll();
                return;
            }

            // 有效性校验（可选扩展）
            if (string.IsNullOrWhiteSpace(url) ||
                !Uri.TryCreate(url, UriKind.Absolute, out _))
            {
                MessageBox.Show("请输入有效的URL地址！",
                    "输入错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                txtUrl.Focus();
                txtUrl.SelectAll();
                return;
            }

            // 保存有效值
            App.ServerUrl = url;
            App.DomainName = domain;
            App.ServerName = name;
            inputDialog.DialogResult = true;
        };

        btnCancel.Click += (s, args) => inputDialog.DialogResult = false;

        // 组合控件
        buttonPanel.Children.Add(btnCancel);
        buttonPanel.Children.Add(btnOk);

        mainStack.Children.Add(spUrl);
        mainStack.Children.Add(spDomain);
        mainStack.Children.Add(spName);
        mainStack.Children.Add(buttonPanel);

        inputDialog.Content = mainStack;

        // 显示对话框
        if (inputDialog.ShowDialog() != true) return;
        MessageBox.Show("配置更新成功！\n需要重启程序才能生效！", "系统提示",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
        App.UpdateUser(); //写到本地
        AppUtil.Restart(); //重启
    }
}