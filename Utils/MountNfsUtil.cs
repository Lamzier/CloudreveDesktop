using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using System.Windows;
using CloudreveDesktop.CloudreveApi;
using CloudreveDesktop.pojo;
using DokanNet;
using Microsoft.Win32;

namespace CloudreveDesktop.utils;

public static class MountNfsUtil
{
    private const int RequiredMajor = 2;
    private const int RequiredMinor = 0;
    private const int RequiredBuild = 6;
    private const int RequiredRevision = 1000;

    public static void Init()
    {
        try
        {
            // 检查Dokan驱动安装状态
            if (!CheckDokanInstallation())
            {
                MessageBox.Show("Dokan 文件系统驱动未安装",
                    "请先安装 Dokan Library 2.3.0.1000 版本");
                Application.Current.Shutdown(); // 关闭程序
                return;
            }

            // 获取实际版本
            var actualVersion = GetActualDriverVersion();
            var apiVersion = GetApiVersion();
            if (actualVersion != null && apiVersion != null) return;
            MessageBox.Show("Dokan 文件系统驱动未安装",
                "请先安装 Dokan Library 2.3.0.1000 版本");
            Application.Current.Shutdown(); // 关闭程序
        }
        catch (Exception ex)
        {
            MessageBox.Show("初始化失败", $"Dokan 驱动检查失败: {ex.Message}");
        }
    }

    private static Version GetApiVersion()
    {
        // 反射获取程序集版本
        var assembly = typeof(Dokan).Assembly;
        var version = assembly.GetName().Version;

        // 如果无法获取则返回默认版本
        return version ?? new Version(0, 0);
    }

    private static Version GetActualDriverVersion()
    {
        try
        {
            var version = DokanDriverVersion();
            // 正确的位分解方式（Big-endian）
            return new Version(
                (int)((version >> 48) & 0xFFFF), // Major
                (int)((version >> 32) & 0xFFFF), // Minor
                (int)((version >> 16) & 0xFFFF), // Build
                (int)(version & 0xFFFF)); // Revision
        }
        catch (DllNotFoundException)
        {
            // 回退到注册表检查
            var registryVersion = CheckRegistryVersion();
            return registryVersion.version;
        }
    }

    private static bool CheckDokanInstallation()
    {
        // 双重检查机制
        return CheckDokanRegistry() || CheckDokanFileSystem();
    }

    private static bool CheckDokanRegistry()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\WOW6432Node\Dokan\DokanLibrary");
            return key?.GetValue("Version") != null;
        }
        catch
        {
            return false;
        }
    }

    private static bool CheckDokanFileSystem()
    {
        var systemPaths = new[]
        {
            Environment.SystemDirectory,
            Environment.GetFolderPath(Environment.SpecialFolder.SystemX86)
        };

        foreach (var path in systemPaths)
        {
            var dllPath = Path.Combine(path, "dokan2.dll");
            if (File.Exists(dllPath)) return true;
        }

        return false;
    }

    [DllImport("dokan2.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern ulong DokanDriverVersion();


    private static (bool isValid, Version version) CheckRegistryVersion()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\WOW6432Node\Dokan\DokanLibrary");

            if (key?.GetValue("Version") is string versionStr &&
                Version.TryParse(versionStr, out var version))
                return (version >= new Version(RequiredMajor, RequiredMinor, RequiredBuild, RequiredRevision), version);
            return (false, new Version(0, 0));
        }
        catch
        {
            return (false, new Version(0, 0));
        }
    }

    public static async void Mounts()
    {
        Directory.CreateDirectory(App.TempPath); // 创建目录
        foreach (var nfsInfoPojo in App.NfsInfos.Where(nfsInfoPojo => nfsInfoPojo.IsEnable)) await Mount(nfsInfoPojo);
    }

    private static async Task Mount(NfsInfoPojo nfsInfoPojo)
    {
        var mountPath = App.TempPath + $@"\mounts\{nfsInfoPojo.Id}"; // 挂载路径
        // C:\Users\12554\AppData\Local\Temp\CloudreveDesktop\mounts\1
        Directory.CreateDirectory(mountPath); // 创建目录
        // 自定义列名
        // ExplorerUtil.CustomColumnNames(mountPath);


        // 挂载目录
        var files = await GetFiles(nfsInfoPojo.NfsPath);
        UpdateFiles(mountPath, nfsInfoPojo.NfsPath, files); // 更新文件
        // 监听事件
        // DirListener.StartOpenListening(mountPath, nfsInfoPojo.NfsPath);
    }

    // 创建目录
    private static void UpdateFiles(string mountPath, string netPath, List<FilePojo> files)
    {
        var fullPath = Path.Combine(
            mountPath.TrimEnd('\\', '/'),
            netPath.TrimStart('\\', '/')
        ).Replace('/', Path.DirectorySeparatorChar); // 拼接成当前目录
        // 创建文件 / 文件夹
        foreach (var filePojo in files)
            if (filePojo.Type.ToLower().Equals("dir"))
                Directory.CreateDirectory(fullPath + $@"\{filePojo.Name}"); // 创建目录
            else
                using (File.Create(fullPath + $@"\{filePojo.Name}")) // 创建文件
                {
                }
    }

    private static async Task<List<FilePojo>> GetFiles(string nfsPath)
    {
        List<FilePojo> files = [];
        if (CheckJson(await FilesApi.GetDirectory(nfsPath))["data"] is not { } data) return files;
        var objects = data["objects"] as JsonArray;
        files.AddRange(objects!.OfType<JsonNode>()
            .Select(jsonNode => new FilePojo
            {
                Id = (string)jsonNode["id"]!,
                Name = (string)jsonNode["name"]!,
                Path = (string)jsonNode["path"]!,
                Size = (long)jsonNode["size"]!,
                Type = (string)jsonNode["type"]!,
                Date = (DateTimeOffset)jsonNode["date"]!,
                CreateDate = (DateTimeOffset)jsonNode["create_date"]!
            }));
        return files;
    }

    private static JsonNode CheckJson(JsonNode json)
    {
        var code = (int)json["code"]!;
        var msg = (string)json["msg"]!;
        if (code == 0) return json;
        MessageBox.Show(msg, "错误");
        return null!;
    }
}