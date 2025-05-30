using System.IO;
using System.Net.Http;
using System.Security.Principal;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;
using CloudreveDesktop.pojo;
using CloudreveDesktop.utils;

namespace CloudreveDesktop;

/// <summary>
///     Interaction logic for App.xaml
/// </summary>
public partial class App
{
    // 服务器地址
    // public static readonly string ServerUrl = "http://nas.lamzy.cn/";
    public static readonly string ServerUrl = "http://127.0.0.1:5212/";

    public static readonly string DomainName = "nas.lamzy.cn";

    public static readonly string ServerName = "LamNas";

    public static readonly List<NfsInfoPojo> NfsInfos = [];

    // cookies
    public static readonly List<string> Cookies = new();

    // 用户名
    public static string UserName = "";

    // 密码
    public static string Password = "";

    // 我的文档路径
    private static readonly string DocumentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

    // 缓存路径
    public static readonly string TempPath = Path.GetTempPath() + "CloudreveDesktop";

    // 下载路径
    public static readonly string DownloadPath =
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads\CloudreveDesktop";

    // Aes加密默认密匙
    private static readonly string AesPassword = "Lamzy";

    public static readonly HttpClient HttpClient = new();

    // 是否已经登陆
    public static bool IsLoggedIn = false;

    // 执行路径
    public static readonly string FullPath = AppDomain.CurrentDomain.BaseDirectory;


    // 初始化
    public App()
    {
        Instance = this;
        CheckAdministrator(); // 检查管理员权限
        MountNfsInit();
        var userPath = DocumentsPath + @"\CloudreveDesktop\user.lam";
        var directoryPath = Path.GetDirectoryName(userPath);
        if (!string.IsNullOrEmpty(directoryPath)) Directory.CreateDirectory(directoryPath); //创建父目录文件
        if (!File.Exists(userPath))
            using (File.Create(userPath))
            {
            }

        // 读取文件并解密
        var userContent = File.ReadAllText(userPath);
        if (userContent.Length <= 0) userContent = UpdateUser();
        var encrypted = AesUtil.Decrypt(userContent, AesPassword);
        try
        {
            var json = JsonNode.Parse(encrypted)!;
            if (json["Cookies"] != null)
            {
                var cookies = (JsonArray)json["Cookies"]!;
                Cookies.Clear();
                foreach (var jsonNode in cookies)
                    if (jsonNode != null)
                        Cookies.Add(jsonNode.ToString());
            }

            if (json["NfsInfos"] != null)
            {
                var nfsInfos = (JsonArray)json["NfsInfos"]!;
                NfsInfos.Clear();
                foreach (var jsonNode in nfsInfos)
                {
                    if (jsonNode == null) continue;
                    var nfsInfoPojo = new NfsInfoPojo();
                    if (jsonNode["Id"] != null) nfsInfoPojo.Id = (long)jsonNode["Id"]!;
                    if (jsonNode["NfsPath"] != null) nfsInfoPojo.NfsPath = (string)jsonNode["NfsPath"]!;
                    if (jsonNode["IsEnable"] != null) nfsInfoPojo.IsEnable = (bool)jsonNode["IsEnable"]!;
                    if (jsonNode["Date"] != null) nfsInfoPojo.Date = (DateTimeOffset)jsonNode["Date"]!;
                    if (jsonNode["CreateDate"] != null)
                        nfsInfoPojo.CreateDate = (DateTimeOffset)jsonNode["CreateDate"]!;
                    NfsInfos.Add(nfsInfoPojo);
                }
            }

            if (json["userName"] != null) UserName = (string)json["userName"]!;
            if (json["Password"] != null) Password = (string)json["Password"]!;
        }
        catch (Exception e)
        {
            UpdateUser();
        }

        MountNfsUtil.Mounts(); // 挂载硬盘
    }

    private static App Instance { get; set; }

    private void MountNfsInit()
    {
        MountNfsUtil.Init();
    }

    private static void CheckAdministrator()
    {
        if (IsAdministrator()) return;
        MessageBox.Show("请以管理员权限运行！\n否则无法使用磁盘挂载功能！", "信息");
        Instance.Shutdown();
    }

    private static bool IsAdministrator()
    {
        var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    public static string GetCookies()
    {
        return string.Join(";", Cookies);
    }

    // 清除数据，用于注销登陆
    public static void ClearData()
    {
        UserName = "";
        Password = "";
        Cookies.Clear();
        NfsInfos.Clear();
    }

    // 更新用户信息到本地文件
    public static string UpdateUser()
    {
        var userData = new
        {
            Cookies,
            userName = UserName,
            Password,
            NfsInfos = NfsInfos.Select(n => new
            {
                n.Id,
                n.NfsPath,
                n.IsEnable,
                n.Date,
                n.CreateDate
            })
        };
        var userDataJson = JsonSerializer.Serialize(userData);
        var encrypt = AesUtil.Encrypt(userDataJson, AesPassword);
        // 写入到文件
        File.WriteAllText(DocumentsPath + @"\CloudreveDesktop\user.lam", encrypt);
        return encrypt;
    }
}