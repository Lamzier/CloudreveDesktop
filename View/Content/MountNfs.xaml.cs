using System.Collections.ObjectModel;
using System.Windows;
using CloudreveDesktop.pojo;

namespace CloudreveDesktop.View.Content;

public partial class MountNfs
{
    public MountNfs()
    {
        InitializeComponent();
        DataContext = this;

        // 初始化演示数据
        NfsInfoPojos.Add(new NfsInfoPojo
        {
            NfsPath = "/",
            IsEnable = true,
            Date = DateTimeOffset.Now,
            CreateDate = DateTimeOffset.Now.AddDays(-7)
        });

        NfsInfoPojos.Add(new NfsInfoPojo
        {
            NfsPath = "/离线下载",
            IsEnable = false,
            Date = DateTimeOffset.Now.AddHours(-2),
            CreateDate = DateTimeOffset.Now.AddDays(-3)
        });
    }

    public ObservableCollection<NfsInfoPojo> NfsInfoPojos { get; } = [];

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
    }
}