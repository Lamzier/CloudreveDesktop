using System.Collections.ObjectModel;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CloudreveDesktop.CloudreveApi;
using CloudreveDesktop.pojo;

namespace CloudreveDesktop.View.Content;

public partial class MyFiles
{
    private readonly ObservableCollection<FilePojo> FileItems = []; // 读取到的文件列表（同步到UI）

    public MyFiles()
    {
        InitializeComponent();
        Rendering();
        Instance = this;
    }

    public string Path { get; set; }

    public ObservableCollection<string> PathList { get; } = [];

    public static MyFiles Instance { get; private set; } = null!;

    // 渲染数据
    public void Rendering()
    {
        if (App.IsLoggedIn) // 已经登陆了
            InitDirectory("/");
    }

    private async void InitDirectory(string path)
    {
        Path = path;
        var pathList = Path.Split("/");
        PathList.Clear();
        FileItems.Clear();
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


        // uiElementCollection.Add(Instance);
        // <Button Content="/" Style="{StaticResource NavigationButtonStyle}" PreviewMouseLeftButtonDown="Home_Click" />


        var directoryList = await FilesApi.Directory(path);
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
            FilePojo filePojo = new();
            filePojo.Id = (string)jsonNode?["id"]!;
            filePojo.Name = (string)jsonNode?["name"]!;
            filePojo.Path = (string)jsonNode?["path"]!;
            filePojo.Size = (long)jsonNode?["size"]!;
            filePojo.Type = (string)jsonNode?["type"]!;
            filePojo.Date = (DateTimeOffset)jsonNode?["date"]!;
            filePojo.CreateDate = (DateTimeOffset)jsonNode?["create_date"]!;
            FileItems.Add(filePojo);
        }

        FilesListView.ItemsSource = FileItems;
    }

    private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        // 强制ScrollViewer处理滚动
        var scrollViewer = (ScrollViewer)sender;
        scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta);
        e.Handled = true;
    }

    private void ListViewItem_DoubleClick(object sender, MouseButtonEventArgs e)
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
        Console.WriteLine("download");
    }

    private void Home_Click(object sender, MouseButtonEventArgs e)
    {
        InitDirectory("/");
    }

    private void Goto_Click(object sender, MouseButtonEventArgs e)
    {
    }
}