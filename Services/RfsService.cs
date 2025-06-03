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
    private readonly LiteDbStorageService _fileIdsLiteDbStorageService; // 文件ID数据库
    private readonly NfsInfoPojo _nfsInfo;
    private IDokanOperations _dokanOperationsImplementation;


    /**
     * netPath 网络路径 例如： "/"
     * _cachePath 缓存路径
     */
    public RfsService(NfsInfoPojo nfsInfo)
    {
        _nfsInfo = nfsInfo;
        _cachePath = App.TempPath + $@"\mounts\{nfsInfo.Id}"; // 缓存路径
        _fileIdsLiteDbStorageService = new LiteDbStorageService($"FileIds_{nfsInfo.Id}.db");
    }

    public NtStatus CreateFile(string fileName, FileAccess access, FileShare share, FileMode mode, FileOptions options,
        FileAttributes attributes, IDokanFileInfo info)
    {
        var prohibitedFiles = new[] { "desktop.ini", "thumbs.db" };
        if (prohibitedFiles.Contains(Path.GetFileName(fileName).ToLower())) return DokanResult.AccessDenied;

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
        // bytesRead = 0;
        // return NtStatus.Success;
        bytesRead = 0;
        if (info.IsDirectory) return NtStatus.Success;


        if (Path.GetExtension(fileName).ToLower() == ".lnk") return DokanResult.AccessDenied; // 阻止直接访问快捷方式
        if (fileName.ToLower().EndsWith("desktop.ini")) return NtStatus.Success; // 过滤系统文件或临时文件


        // 路径安全检查
        var sanitizedPath = fileName.Replace("\\", "/").TrimStart('/');
        if (sanitizedPath.Contains(".."))
        {
            Console.WriteLine($"非法路径: {fileName}");
            return DokanResult.AccessDenied;
        }

        // 从数据库获取
        var fileId = _fileIdsLiteDbStorageService.Get(fileName.Replace("\\", "/"));
        var json = JsonNode.Parse(fileId)!;
        // 查看缓存是否存在
        // 构建缓存路径
        var cacheFilePath = Path.Combine(_cachePath, sanitizedPath);
        if (!File.Exists(cacheFilePath)) // 不存在就在网上拉取，不用检查时间（时间有BUG）
        {
            var creationTimeNet = (DateTime)json["create_date"]!;
            var lastWriteTimeNet = (DateTime)json["date"]!;
            var jsonKey = FilesApi.GetDownloadKey((string)json["id"]!).GetAwaiter().GetResult();
            var resultJson = (string)ResultJson(jsonKey)!;
            // 下载到缓存
            if (resultJson.StartsWith("/")) resultJson = resultJson.AsSpan()[1..].ToString();
            FilesApi.DownloadFile(App.ServerUrl + resultJson, cacheFilePath, lastWriteTimeNet, creationTimeNet)
                .GetAwaiter().GetResult();
            // 校验文件
            var expectedSize = (long)json["size"]!;
            var actualSize = new FileInfo(cacheFilePath).Length;
            if (actualSize != expectedSize) File.Delete(cacheFilePath);
        }
        // 读取逻辑

        using var fs = new FileStream(
            cacheFilePath,
            FileMode.Open,
            System.IO.FileAccess.Read,
            FileShare.ReadWrite,
            4096, // 缓冲区
            FileOptions.SequentialScan);

        if (offset > 0) fs.Seek(offset, SeekOrigin.Begin);

        var totalRead = 0;
        while (totalRead < buffer.Length)
        {
            var read = fs.Read(
                buffer,
                totalRead,
                Math.Min(buffer.Length - totalRead, 4096));

            if (read == 0) break;

            totalRead += read;
        }

        bytesRead = totalRead;

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

    // public NtStatus GetFileInformation(
    //     string filename,
    //     out FileInformation fileinfo,
    //     IDokanFileInfo info)
    // {
    //     fileinfo = new FileInformation
    //     {
    //         FileName = filename,
    //         Attributes = FileAttributes.Directory,
    //         LastAccessTime = DateTime.Now,
    //         LastWriteTime = DateTime.Now,
    //         CreationTime = DateTime.Now
    //     };
    //
    //     return DokanResult.Success;
    // }

    public NtStatus GetFileInformation(string fileName, out FileInformation fileInfo, IDokanFileInfo info)
    {
        fileInfo = new FileInformation { FileName = fileName };
        // 根目录处理
        if (fileName == "\\")
        {
            fileInfo.Attributes = FileAttributes.Directory;
            fileInfo.CreationTime = fileInfo.LastAccessTime = fileInfo.LastWriteTime = DateTime.Now;
            return DokanResult.Success;
        }

        try
        {
            var sanitizedPath = fileName.Replace("\\", "/").TrimStart('/');
            sanitizedPath = $"/{sanitizedPath}";
            var fileData = _fileIdsLiteDbStorageService.Get(sanitizedPath);
            if (string.IsNullOrEmpty(fileData)) return DokanResult.FileNotFound;
            var json = JsonNode.Parse(fileData);
            var isDirectory = json?["type"]?.GetValue<string>().ToLower() == "dir";
            fileInfo.Attributes = isDirectory ? FileAttributes.Directory : FileAttributes.Normal;
            fileInfo.Length = isDirectory ? 0 : (long)json!["size"]!;
            fileInfo.CreationTime = (DateTime)json!["create_date"]!;
            fileInfo.LastWriteTime = (DateTime)json["date"]!;
            fileInfo.LastAccessTime = DateTime.Now;
            return DokanResult.Success;
        }
        catch
        {
            return DokanResult.Unsuccessful;
        }
    }


    public NtStatus FindFiles(string fileName, out IList<FileInformation> files, IDokanFileInfo info)
    {
        Console.WriteLine("FindFiles");
        files = null!;
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
            _fileIdsLiteDbStorageService.Put(key, jsonNode.ToJsonString()); // 写入键值到数据库
        }

        return NtStatus.Success;
    }

    public NtStatus SetFileAttributes(string fileName, FileAttributes attributes, IDokanFileInfo info)
    {
        return NtStatus.Success;
    }

    public NtStatus SetFileTime(string fileName, DateTime? creationTime, DateTime? lastAccessTime,
        DateTime? lastWriteTime,
        IDokanFileInfo info)
    {
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
        var oldDirectoryName = Path.GetDirectoryName(oldName);
        var newDirectoryName = Path.GetDirectoryName(newName);
        if (oldDirectoryName != null && oldDirectoryName.Equals(newDirectoryName))
            return ReName(oldName, newName, replace, info); // 重命名操作

        // 移动操作
        // Console.WriteLine(oldName);
        // Console.WriteLine(newName);
        // Console.WriteLine(oldDirectoryName);
        // Console.WriteLine(newDirectoryName);
        // Console.WriteLine(replace);

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
        _fileIdsLiteDbStorageService.Dispose();
        return NtStatus.Success;
    }

    public NtStatus FindStreams(string fileName, out IList<FileInformation> streams, IDokanFileInfo info)
    {
        Console.WriteLine("FindStreams");
        streams = null!;
        return NtStatus.Success;
    }

    private NtStatus ReName(string oldName, string newName, bool replace, IDokanFileInfo info)
    {
        // 检查是否有重名（本地检查，服务器检查）
        var newFileJson = _fileIdsLiteDbStorageService.Get(newName.Replace("\\", "/"));
        if (!string.IsNullOrEmpty(newFileJson)) return NtStatus.ObjectNameCollision;
        var oldFileJson = _fileIdsLiteDbStorageService.Get(oldName.Replace("\\", "/"));
        if (string.IsNullOrEmpty(oldFileJson) || !File.Exists(_cachePath + oldName))
            // 不存在文件
            return NtStatus.NoSuchFile;

        // 校验完成添加异步任务
        TaskService.Instance.AddReNameTask(oldName, newName);
        // 修改本地缓存文件
        File.Move(_cachePath + oldName, _cachePath + newName);
        return NtStatus.Success;
    }


    private static JsonNode ResultJson(JsonNode json)
    {
        JsonNode result = new JsonObject();
        var code = (int)json["code"]!;
        if (code != 0)
            return result;
        var data = json["data"];
        return data ?? result;
    }
}