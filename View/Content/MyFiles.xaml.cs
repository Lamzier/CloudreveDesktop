using System.Collections.ObjectModel;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CloudreveDesktop.CloudreveApi;
using CloudreveDesktop.pojo;
using CloudreveDesktop.utils;

namespace CloudreveDesktop.View.Content;

public partial class MyFiles
{
    private readonly ObservableCollection<FilePojo> _fileItems = []; // 读取到的文件列表（同步到UI）

    public MyFiles()
    {
        InitializeComponent();
        Rendering();
        Instance = this;
    }

    private string Path { get; set; } = null!;

    private ObservableCollection<string> PathList { get; } = [];

    public static MyFiles Instance { get; private set; } = null!;

    // 渲染数据
    private void Rendering()
    {
        if (App.IsLoggedIn) // 已经登陆了
            InitDirectory("/");
    }

    private async void InitDirectory(string path)
    {
        Path = path;
        var pathList = Path.Split("/");
        PathList.Clear();
        _fileItems.Clear();
        pathList.ToList().ForEach(se => PathList.Add(se));

        DockPanelNav.Children.Clear();
        for (var i = 0; i < PathList.Count; i++)
        {
            var se = PathList[i];
            if (se == string.Empty) continue;
            var button = new Button
            {
                Content = se,
                Style = (Style)FindResource("NavigationButtonStyle")
            };
            var index = i;
            button.PreviewMouseLeftButtonDown += (_, _) => Goto_Click(index);
            var button2 = new Button
            {
                Content = ">",
                Style = (Style)FindResource("NavigationButtonStyle"),
                IsEnabled = false
            };
            DockPanelNav.Children.Add(button);
            if (i >= pathList.Length - 1) continue;
            DockPanelNav.Children.Add(button2);
        }

        var directoryList = await FilesApi.GetDirectory(path);
        var code = (int)directoryList["code"]!;
        var msg = (string)directoryList["msg"]!;
        if (code != 0)
        {
            MessageBox.Show(code + "：" + msg, "错误"); //其他异常
            return;
        }

        var data = directoryList["data"]!;
        var objects = (JsonArray)data["objects"]!;


        foreach (var jsonNode in objects)
        {
            FilePojo filePojo = new()
            {
                Id = (string)jsonNode?["id"]!,
                Name = (string)jsonNode?["name"]!,
                Path = (string)jsonNode?["path"]!,
                Size = (long)jsonNode?["size"]!,
                Type = (string)jsonNode?["type"]!,
                Date = (DateTimeOffset)jsonNode?["date"]!,
                CreateDate = (DateTimeOffset)jsonNode?["create_date"]!
            };
            _fileItems.Add(filePojo);
        }

        FilesListView.ItemsSource = _fileItems;
    }

    private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        // 强制ScrollViewer处理滚动
        var scrollViewer = (ScrollViewer)sender;
        scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta);
        e.Handled = true;
    }

    private async void ListViewItem_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (FilesListView.SelectedItem is not FilePojo item) return;
        var path = item.Path;
        var name = item.Name;
        if (item.Type.ToLower().Equals("dir"))
        {
            //跳转到下一级目录
            string url;
            if (path.Equals("/"))
                url = path + name;
            else
                url = path + "/" + name;
            InitDirectory(url);
            return;
        }

        // 文件 ，执行下载
        var json = await FilesApi.GetDownloadKey(item.Id);
        var code = (int)json["code"]!;
        var msg = (string)json["msg"]!;
        if (code != 0)
        {
            MessageBox.Show(msg, "错误");
            return;
        }

        var data = (string)json["data"]!;
        if (data.StartsWith("/")) data = data.Substring(1);
        var downloadUrl = App.ServerUrl + data; // 文件下载Url

        var state = await FilesApi.DownloadFile(downloadUrl, App.DownloadPath + "/" + item.Name);

        // 添加完成下载提示和音效
        if (state)
        {
            MessageBox.Show("文件已成功下载到：" + App.DownloadPath + "/" + item.Name, "下载完成");
            Mp3Util.Play("download");
        }
        else
        {
            MessageBox.Show("文件下载失败，请重试。", "下载失败");
            Mp3Util.Play("error");
        }
    }

    private void Home_Click(object sender, MouseButtonEventArgs e)
    {
        InitDirectory("/");
    }

    private void Goto_Click(int index)
    {
        if (index >= PathList.Count - 1) return;
        var path = "";
        for (var i = 0; i < index; i++)
        {
            path += "/";
            path += PathList[i + 1];
        }

        InitDirectory(path); // 跳转到目录
    }
}