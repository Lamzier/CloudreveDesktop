﻿using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using CloudreveDesktop.CloudreveApi;
using CloudreveDesktop.utils;

namespace CloudreveDesktop;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();
        Hide(); // 隐藏
        CheckLogin(); //检查登陆 + 渲染数据
        Instance = this;
        // 初始化就隐藏 
    }

    public static MainWindow Instance { get; private set; } = null!;

    // 渲染数据
    public void Rendering()
    {
        if (App.IsLoggedIn) // 已经登陆了
        {
            InitUserInfoStorage();
            ContentFrame.Source = new Uri("View/Content/MyFiles.xaml", UriKind.Relative);
        }
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
        StorageFree.Text = "剩余：" + freeGStr;
        StorageTotal.Text = totalTStr;
        StorageUsed.Text = usedGStr;
    }


    private async void CheckLogin()
    {
        var userSetting = await UserApi.GetUserSetting();
        var code = (int)userSetting["code"]!;
        var msg = (string)userSetting["msg"]!;

        switch (code)
        {
            case 401:
                new LoginWindow().ShowDialog(); // 没登录 阻塞显示
                return;
            case -1:
                MessageBox.Show("目标服务器连接失败！", "信息");
                new LoginWindow().ShowDialog(); // 服务器网络异常
                return;
        }

        if (code != 0)
        {
            MessageBox.Show(code + "：" + msg, "错误"); //其他异常
            return;
        }

        // 已经登陆
        App.IsLoggedIn = true;
        Show();
        Rendering(); //渲染数据
    }

    // 设置
    private void Setting_Click(object sender, RoutedEventArgs e)
    {
        var proc = new Process();
        proc.StartInfo.FileName = App.ServerUrl + "setting";
        proc.StartInfo.UseShellExecute = true;
        proc.Start();
    }

    // 用户中心
    private void UserCenter_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show("是否注销登陆？", "信息", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.No) return;
        App.ClearData();
        App.UpdateUser();
        // 重新启动软件
        AppUtil.Restart();
    }

    private void MyShare_Click(object sender, MouseButtonEventArgs e)
    {
        ContentFrame.Source = new Uri("View/Content/MyShare.xaml", UriKind.Relative);
    }

    private void MyFiles_Click(object sender, MouseButtonEventArgs e)
    {
        ContentFrame.Source = new Uri("View/Content/MyFiles.xaml", UriKind.Relative);
    }

    private void MountNfs_Click(object sender, MouseButtonEventArgs e)
    {
        ContentFrame.Source = new Uri("View/Content/MountNfs.xaml", UriKind.Relative);
    }
}