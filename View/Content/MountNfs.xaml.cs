using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using CloudreveDesktop.pojo;
using CloudreveDesktop.utils;
using Microsoft.VisualBasic;

namespace CloudreveDesktop.View.Content;

public partial class MountNfs
{
    public MountNfs()
    {
        InitializeComponent();
        DataContext = this;
        foreach (var nfsInfoPojo in App.NfsInfos) NfsInfoPojos.Add(nfsInfoPojo);
    }

    public ObservableCollection<NfsInfoPojo> NfsInfoPojos { get; } = [];

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: long id }) return;
        for (var i = 0; i < NfsInfoPojos.Count; i++)
        {
            var nfsInfoPojo = NfsInfoPojos[i];
            if (!nfsInfoPojo.Id.Equals(id)) continue;
            NfsInfoPojos.RemoveAt(i);
        }

        //修改到全局
        App.NfsInfos.Clear();
        App.NfsInfos.AddRange(NfsInfoPojos);
        App.UpdateUser();
        MountNfsUtil.Mounts(); // 重新挂载
    }

    private void Enable_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: long id }) return;
        for (var i = 0; i < NfsInfoPojos.Count; i++)
        {
            var nfsInfoPojo = NfsInfoPojos[i];
            if (!nfsInfoPojo.Id.Equals(id)) continue;
            nfsInfoPojo.IsEnable = !nfsInfoPojo.IsEnable;
            NfsInfoPojos.RemoveAt(i);
            NfsInfoPojos.Insert(i, nfsInfoPojo);
        }

        //修改到全局
        App.NfsInfos.Clear();
        App.NfsInfos.AddRange(NfsInfoPojos);
        App.UpdateUser();
        MountNfsUtil.Mounts(); // 重新挂载
    }

    private void AddMountNfs_Click(object sender, RoutedEventArgs e)
    {
        var path = Interaction.InputBox("请输入挂载路径：\n例1：/\n例2：/目录1/目录2", "添加路径");
        if (string.IsNullOrWhiteSpace(path)) return;
        var drive = Interaction.InputBox("请输入挂载的盘符：\n例1：Z:\\", "挂载盘符");
        if (string.IsNullOrWhiteSpace(drive)) return;
        var name = Interaction.InputBox("请输入挂载的盘符名称\n例1：电脑同步", "设置名称");
        if (string.IsNullOrWhiteSpace(name)) return;
        var enable = MessageBox.Show("是否启用该路径？", "确认", MessageBoxButton.YesNo) == MessageBoxResult.Yes;
        long id = 0;
        foreach (var nfsInfoPojo in NfsInfoPojos)
            if (nfsInfoPojo.Id >= id)
                id = nfsInfoPojo.Id;
        id++;
        NfsInfoPojos.Add(new NfsInfoPojo
        {
            Id = id, Name = name, NfsPath = path, IsEnable = enable, Date = DateTime.Now, CreateDate = DateTime.Now,
            Drive = drive
        });
        //NfsInfoPojos 检查 NfsPath合法性
        //修改到全局
        App.NfsInfos.Clear();
        App.NfsInfos.AddRange(NfsInfoPojos);
        App.UpdateUser();
        MountNfsUtil.Mounts(); // 重新挂载
    }
}