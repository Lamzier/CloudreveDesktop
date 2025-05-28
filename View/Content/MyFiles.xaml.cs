using System.Collections.ObjectModel;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CloudreveDesktop.CloudreveApi;
using CloudreveDesktop.pojo;
using CloudreveDesktop.utils;
using Microsoft.VisualBasic;
using Microsoft.Win32;

namespace CloudreveDesktop.View.Content;

public partial class MyFiles
{
    private readonly ObservableCollection<FilePojo> _fileItems = []; // 读取到的文件列表（同步到UI）

    private string _dirId = ""; //当前文件夹的Id

    private string _policyId = ""; // 存储策略id

    public MyFiles()
    {
        InitializeComponent();
        Rendering();
    }

    // 当前路径
    private string DirPath { get; set; } = null!;

    private ObservableCollection<string> DirPathList { get; } = [];

    private void AddFile_Click(object sender, MouseButtonEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "请选择要上传的文件",
            Filter = "所有文件 (*.*)|*.*", // 你可以限制特定类型
            Multiselect = false // 是否允许多选
        };
        // 显示对话框并判断是否点击了“确定”
        if (dialog.ShowDialog() == true)
        {
            var selectedFilePath = dialog.FileName; // 选择了文件
            // 上传文件
            AddFile(selectedFilePath);
        }
    }

    private async void AddFile(string selectedFilePath)
    {
        var json = await FilesApi.UploadFileGetSessionId(DirPath, _policyId, selectedFilePath);
        var code = (int)json["code"]!;
        var msg = (string)json["msg"]!;
        if (code != 0)
        {
            MessageBox.Show(msg, "错误");
            return;
        }

        var data = json["data"]!;
        var sessionId = (string)data["sessionID"]!;
        var uploadJson = await FilesApi.UploadFile(selectedFilePath, sessionId);
        code = (int)uploadJson["code"]!;
        msg = (string)uploadJson["msg"]!;
        if (code != 0) MessageBox.Show(msg, "错误");
        // 刷新界面
        Refresh();
    }

    // 渲染数据
    private void Rendering()
    {
        if (App.IsLoggedIn) // 已经登陆了
            InitDirectory("/");
    }

    private async void InitDirectory(string path)
    {
        DirPath = path;
        var pathList = DirPath.Split("/");
        DirPathList.Clear();
        _fileItems.Clear();
        pathList.ToList().ForEach(se => DirPathList.Add(se));
        _dirId = "";
        _policyId = "";
        DockPanelNav.Children.Clear();
        for (var i = 0; i < DirPathList.Count; i++)
        {
            var se = DirPathList[i];
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
        _dirId = (string)data["parent"]!;
        var policy = data["policy"]!; // 存储策略
        _policyId = (string)policy["id"]!;
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
        if (index >= DirPathList.Count - 1) return;
        var path = "";
        for (var i = 0; i < index; i++)
        {
            path += "/";
            path += DirPathList[i + 1];
        }

        InitDirectory(path); // 跳转到目录
    }

    // 刷新界面
    private void Refresh()
    {
        InitDirectory(DirPath);
    }

    private async void DeleteFile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { Tag: FilePojo filePojo }) return;
        var confirm = MessageBox.Show(
            $"确定要删除 [{filePojo.Type}] 类型的文件吗？\n文件名：{filePojo.Name}\n文件ID：{filePojo.Id}",
            "删除确认",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes) return;
        //删除文件
        JsonNode json;
        if (filePojo.Type.ToLower().Equals("dir"))
            json = await FilesApi.DeleteFile(null!, filePojo.Id);
        else
            json = await FilesApi.DeleteFile(filePojo.Id, null!);
        var code = (int)json["code"]!;
        var msg = (string)json["msg"]!;
        if (code != 0)
        {
            MessageBox.Show(msg, "错误");
            return;
        }

        // 刷新页面
        Refresh();
    }

    private async void ReName_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not FilePojo filePojo) return;
        if (Interaction.InputBox("重命名", "新名称：", filePojo.Name) is not { } newName
            || string.IsNullOrWhiteSpace(newName)) return;
        newName = newName.Trim();
        if (filePojo.Name.Equals(newName)) return;
        var json = await FilesApi.ReName(filePojo.Id, filePojo.Type, newName);
        var code = (int)json["code"]!;
        var msg = (string)json["msg"]!;
        if (code != 0)
        {
            MessageBox.Show(msg, "错误");
            return;
        }

        // 刷新页面
        Refresh();
    }

    private async void NewDir_Click(object sender, RoutedEventArgs e)
    {
        if (Interaction.InputBox("新建文件夹", "新建文件夹") is not { } newDir
            || string.IsNullOrWhiteSpace(newDir)) return;
        var safeDirPath = DirPath.TrimEnd('/') ?? "";
        var newPath = $"{safeDirPath}/{newDir.TrimStart('/')}";
        var json = await FilesApi.NewDir(newPath);
        var code = (int)json["code"]!;
        var msg = (string)json["msg"]!;
        if (code != 0)
        {
            MessageBox.Show(msg, "错误");
            return;
        }

        // 刷新页面
        Refresh();
    }
}