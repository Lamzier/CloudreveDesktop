using System.IO;
using System.Security.AccessControl;
using System.Text.Json.Nodes;
using CloudreveDesktop.CloudreveApi;
using CloudreveDesktop.pojo;
using DokanNet;
using FileAccess = DokanNet.FileAccess;

namespace CloudreveDesktop.Services;

/**
 * 面向资源管理服务
 */
public class RfsService : IDokanOperations
{
    private readonly string _cachePath; // 缓存路径
    private readonly LiteDbStorageService _FileIdsLiteDbStorageService; // 文件ID数据库
    private readonly NfsInfoPojo _nfsInfo;


    /**
     * netPath 网络路径 例如： "/"
     * _cachePath 缓存路径
     */
    public RfsService(NfsInfoPojo nfsInfo)
    {
        _nfsInfo = nfsInfo;
        _cachePath = App.TempPath + $@"\mounts\{nfsInfo.Id}"; // 缓存路径
        _FileIdsLiteDbStorageService = new LiteDbStorageService($"FileIds_{nfsInfo.Id}.db");
    }

    public NtStatus CreateFile(string fileName, FileAccess access, FileShare share, FileMode mode, FileOptions options,
        FileAttributes attributes, IDokanFileInfo info)
    {
        // 处理目录创建请求
        if (mode == FileMode.CreateNew &&
            attributes.HasFlag(FileAttributes.Directory)) return NtStatus.Success; // 虚拟目录始终创建成功


        return NtStatus.Success;


        // Console.WriteLine("CreateFile");
        // return NtStatus.Success;
    }

    public void Cleanup(string fileName, IDokanFileInfo info)
    {
    }

    public void CloseFile(string fileName, IDokanFileInfo info)
    {
    }

    public NtStatus ReadFile(string fileName, byte[] buffer, out int bytesRead, long offset, IDokanFileInfo info)
    {
        if (info.IsDirectory)
        {
            bytesRead = 0;
            return NtStatus.Success;
        }

        if (fileName.ToLower().EndsWith("desktop.ini"))
        {
            bytesRead = 0;
            return NtStatus.Success;
        }

        // Console.WriteLine("ReadFile");
        // Console.WriteLine(fileName);

        // 从数据库获取
        var fileId = _FileIdsLiteDbStorageService.Get(fileName.Replace("\\", "/"));
        var json = JsonNode.Parse(fileId)!;
        // 从net获取数据
        var jsonKey = FilesApi.GetDownloadKey((string)json["id"]!).GetAwaiter().GetResult();
        var resultJson = (string)ResultJson(jsonKey)!;
        // 下载到缓存
        if (resultJson.StartsWith("/")) resultJson = resultJson.AsSpan()[1..].ToString();
        var cacheFilePath = _cachePath + fileName;
        FilesApi.DownloadFile(App.ServerUrl + resultJson, cacheFilePath)
            .GetAwaiter().GetResult();
        // 读取缓存文件

        using (var fs = new FileStream(cacheFilePath, FileMode.Open, System.IO.FileAccess.Read, FileShare.Read))
        {
            // 处理偏移量
            if (offset > 0) fs.Seek(offset, SeekOrigin.Begin);

            // 读取到缓冲区
            bytesRead = fs.Read(buffer, 0, buffer.Length);
        }

        return NtStatus.Success;
    }

    public NtStatus WriteFile(string fileName, byte[] buffer, out int bytesWritten, long offset, IDokanFileInfo info)
    {
        Console.WriteLine("WriteFile");
        bytesWritten = 0;
        return NtStatus.Success;
    }

    public NtStatus FlushFileBuffers(string fileName, IDokanFileInfo info)
    {
        Console.WriteLine("FlushFileBuffers");
        return NtStatus.Success;
    }

    public NtStatus GetFileInformation(string fileName, out FileInformation fileInfo, IDokanFileInfo info)
    {
        fileInfo = new FileInformation
        {
            FileName = fileName,
            Attributes = FileAttributes.Directory,
            LastAccessTime = DateTime.Now,
            LastWriteTime = null,
            CreationTime = null
        };


        return DokanResult.Success;
    }

    public NtStatus FindFiles(string fileName, out IList<FileInformation> files, IDokanFileInfo info)
    {
        Console.WriteLine("FindFiles");
        files = null;
        return NtStatus.Success;
    }

    public NtStatus FindFilesWithPattern(string fileName, string searchPattern, out IList<FileInformation> files,
        IDokanFileInfo info)
    {
        var directory = FilesApi.GetDirectory(fileName.Replace("\\", "/")).GetAwaiter().GetResult();
        var resultJson = ResultJson(directory);
        var objects = (JsonArray)resultJson["objects"]!;
        files = new List<FileInformation>();
        foreach (var jsonNode in objects)
        {
            if (jsonNode == null) continue;
            var fileInformation = new FileInformation
            {
                FileName = (string)jsonNode["name"]!,
                Length = (long)jsonNode["size"]!,
                CreationTime = (DateTime)jsonNode["create_date"]!,
                LastAccessTime = (DateTime)jsonNode["date"]!,
                LastWriteTime = (DateTime)jsonNode["date"]!,
                Attributes = ((string)jsonNode["type"])!.ToLower().Equals("dir")
                    ? FileAttributes.Directory
                    : FileAttributes.None
            };
            files.Add(fileInformation);
            var key = $"{fileName.Replace("\\", "/").TrimEnd('/')}/{(string)jsonNode["name"]!}".Replace("//", "/");
            _FileIdsLiteDbStorageService.Put(key, jsonNode.ToJsonString()); // 写入键值到数据库
        }


        return NtStatus.Success;
    }

    public NtStatus SetFileAttributes(string fileName, FileAttributes attributes, IDokanFileInfo info)
    {
        Console.WriteLine("SetFileAttributes");
        return NtStatus.Success;
    }

    public NtStatus SetFileTime(string fileName, DateTime? creationTime, DateTime? lastAccessTime,
        DateTime? lastWriteTime,
        IDokanFileInfo info)
    {
        Console.WriteLine("SetFileTime");
        return NtStatus.Success;
    }

    public NtStatus DeleteFile(string fileName, IDokanFileInfo info)
    {
        Console.WriteLine("DeleteFile");
        return NtStatus.Success;
    }

    public NtStatus DeleteDirectory(string fileName, IDokanFileInfo info)
    {
        Console.WriteLine("DeleteDirectory");
        return NtStatus.Success;
    }

    public NtStatus MoveFile(string oldName, string newName, bool replace, IDokanFileInfo info)
    {
        Console.WriteLine("MoveFile");
        return NtStatus.Success;
    }

    public NtStatus SetEndOfFile(string fileName, long length, IDokanFileInfo info)
    {
        Console.WriteLine("SetEndOfFile");
        return NtStatus.Success;
    }

    public NtStatus SetAllocationSize(string fileName, long length, IDokanFileInfo info)
    {
        Console.WriteLine("SetAllocationSize");
        return NtStatus.Success;
    }

    public NtStatus LockFile(string fileName, long offset, long length, IDokanFileInfo info)
    {
        Console.WriteLine("LockFile");
        return NtStatus.Success;
    }

    public NtStatus UnlockFile(string fileName, long offset, long length, IDokanFileInfo info)
    {
        Console.WriteLine("UnlockFile");
        return NtStatus.Success;
    }

    public NtStatus GetDiskFreeSpace(out long freeBytesAvailable, out long totalNumberOfBytes,
        out long totalNumberOfFreeBytes,
        IDokanFileInfo info)
    {
        var userStorage = UserApi.GetUserStorage().GetAwaiter().GetResult();
        var resultJson = ResultJson(userStorage);
        freeBytesAvailable = (long)resultJson["free"]!; // 可用空间
        totalNumberOfBytes = (long)resultJson["total"]!; // 总空间
        totalNumberOfFreeBytes = freeBytesAvailable;
        return NtStatus.Success;
    }

    public NtStatus GetVolumeInformation(out string volumeLabel, out FileSystemFeatures features,
        out string fileSystemName,
        out uint maximumComponentLength, IDokanFileInfo info)
    {
        volumeLabel = _nfsInfo.Name; // 卷名称
        fileSystemName = App.DomainName; // 显示服务器地址
        maximumComponentLength = 0;
        features = FileSystemFeatures.None;
        return NtStatus.Success;
    }

    public NtStatus GetFileSecurity(string fileName, out FileSystemSecurity? security, AccessControlSections sections,
        IDokanFileInfo info)
    {
        // 创建允许完全控制的安全描述符
        var securityDescriptor = new FileSecurity();

        // 添加Everyone完全控制规则
        securityDescriptor.AddAccessRule(
            new FileSystemAccessRule(
                "Everyone",
                FileSystemRights.FullControl,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                PropagationFlags.None,
                AccessControlType.Allow
            )
        );

        security = securityDescriptor;
        return NtStatus.Success;
    }

    public NtStatus SetFileSecurity(string fileName, FileSystemSecurity security, AccessControlSections sections,
        IDokanFileInfo info)
    {
        Console.WriteLine("SetFileSecurity");
        return NtStatus.Success;
    }

    public NtStatus Mounted(string mountPoint, IDokanFileInfo info)
    {
        // Console.WriteLine("Mounted");
        return NtStatus.Success;
    }

    public NtStatus Unmounted(IDokanFileInfo info)
    {
        Console.WriteLine("Unmounted");
        _FileIdsLiteDbStorageService.Dispose();
        return NtStatus.Success;
    }

    public NtStatus FindStreams(string fileName, out IList<FileInformation> streams, IDokanFileInfo info)
    {
        Console.WriteLine("FindStreams");
        streams = null;
        return NtStatus.Success;
    }

    private long GetFileSizeFromDb(string fileName)
    {
        var fileData = _FileIdsLiteDbStorageService.Get(fileName.Replace("\\", "/"));
        return (long)JsonNode.Parse(fileData)!["size"]!;
    }

    private static JsonNode ResultJson(JsonNode json)
    {
        JsonNode result = new JsonObject();
        var code = (int)json["code"]!;
        var msg = (string)json["msg"]!;
        if (code != 0)
            return result;

        var data = json["data"];
        return data ?? result;
    }
}