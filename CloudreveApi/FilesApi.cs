using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using CloudreveDesktop.utils;

namespace CloudreveDesktop.CloudreveApi;

public static class FilesApi
{
    // 获取文件夹内容
    public static async Task<JsonNode> GetDirectory(string path)
    {
        var pathUri = Uri.EscapeDataString(path); // uri 编码
        var cookies = App.GetCookies();
        if (cookies.Length <= 0) return ResultUtil.GetErrorJson();
        App.HttpClient.DefaultRequestHeaders.Add("cookie", cookies);
        var response = await App.HttpClient.GetAsync(App.ServerUrl + "api/v3/directory" + pathUri);
        var responseString = await response.Content.ReadAsStringAsync();
        var json = JsonNode.Parse(responseString)!;
        return json;
    }

    public static async Task<JsonNode> GetDownloadKey(string fileId)
    {
        var cookies = App.GetCookies();
        if (cookies.Length <= 0) return ResultUtil.GetErrorJson();
        App.HttpClient.DefaultRequestHeaders.Add("cookie", cookies);
        var response = await App.HttpClient.PutAsync(App.ServerUrl + "api/v3/file/download/" + fileId, null);
        var responseString = await response.Content.ReadAsStringAsync();
        var json = JsonNode.Parse(responseString)!;
        return json;
    }

    public static async Task<bool> DownloadFile(string url, string savePath)
    {
        var cookies = App.GetCookies();
        if (cookies.Length <= 0) return false;
        App.HttpClient.DefaultRequestHeaders.Add("cookie", cookies);
        try
        {
            var parentDir = Path.GetDirectoryName(savePath);
            if (!string.IsNullOrEmpty(parentDir)) Directory.CreateDirectory(parentDir); //创建父目录文件
            using var response = await App.HttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            await using var fileStream = File.Create(savePath);
            await response.Content.CopyToAsync(fileStream);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static async Task<JsonNode> ReName(string id, string type, string newName)
    {
        var cookies = App.GetCookies();
        if (cookies.Length <= 0) return ResultUtil.GetErrorJson();
        App.HttpClient.DefaultRequestHeaders.Add("cookie", cookies);
        var data = new
        {
            action = "rename",
            src = new
            {
                dirs = new List<string>(),
                items = new List<string>()
            },
            new_name = newName
        };
        if (type.ToLower().Equals("dir"))
            data.src.dirs.Add(id);
        else
            data.src.items.Add(id);
        var contentJson = JsonSerializer.Serialize(data);
        Console.WriteLine(contentJson);
        var jsonContent = new StringContent(contentJson, Encoding.UTF8, "application/json");
        var response = await App.HttpClient.PostAsync(App.ServerUrl + "api/v3/object/rename", jsonContent);
        var responseString = await response.Content.ReadAsStringAsync();
        var json = JsonNode.Parse(responseString)!;
        return json;
    }

    public static async Task<JsonNode> NewDir(string newPath)
    {
        var cookies = App.GetCookies();
        if (cookies.Length <= 0) return ResultUtil.GetErrorJson();
        App.HttpClient.DefaultRequestHeaders.Add("cookie", cookies);
        var data = new
        {
            path = newPath
        };
        var contentJson = JsonSerializer.Serialize(data);
        var jsonContent = new StringContent(contentJson, Encoding.UTF8, "application/json");
        var response = await App.HttpClient.PutAsync(App.ServerUrl + "api/v3/directory", jsonContent);
        var responseString = await response.Content.ReadAsStringAsync();
        var json = JsonNode.Parse(responseString)!;
        return json;
    }

    public static async Task<JsonNode> DeleteFile(string fileId, string dirId)
    {
        var cookies = App.GetCookies();
        if (cookies.Length <= 0) return ResultUtil.GetErrorJson();
        App.HttpClient.DefaultRequestHeaders.Add("cookie", cookies);
        var data = new
        {
            items = new List<string>(),
            dirs = new List<string>(),
            force = false,
            unlink = false
        };
        if (!string.IsNullOrEmpty(fileId)) data.items.Add(fileId);
        if (!string.IsNullOrEmpty(dirId)) data.dirs.Add(dirId);
        var contentJson = JsonSerializer.Serialize(data);
        var jsonContent = new StringContent(contentJson, Encoding.UTF8, "application/json");
        var response = await HttpUtil.DeleteAsync(App.ServerUrl + "api/v3/object", jsonContent);
        var responseString = await response.Content.ReadAsStringAsync();
        var json = JsonNode.Parse(responseString)!;
        return json;
    }

    public static async Task<JsonNode> UploadFile(string selectedFilePath, string sessionId)
    {
        var cookies = App.GetCookies();
        if (cookies.Length <= 0) return ResultUtil.GetErrorJson();
        App.HttpClient.DefaultRequestHeaders.Add("cookie", cookies);
        var fileBytes = await File.ReadAllBytesAsync(selectedFilePath);
        var fileData = new ByteArrayContent(fileBytes);
        fileData.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        var response = await App.HttpClient.PostAsync(App.ServerUrl + $"api/v3/file/upload/{sessionId}/0", fileData);
        var responseString = await response.Content.ReadAsStringAsync();
        var json = JsonNode.Parse(responseString)!;
        return json;
    }

    // 上传文件获取SessionId
    public static async Task<JsonNode> UploadFileGetSessionId(string path, string fileId, string selectedFilePath)
    {
        var cookies = App.GetCookies();
        if (cookies.Length <= 0) return ResultUtil.GetErrorJson();
        App.HttpClient.DefaultRequestHeaders.Add("cookie", cookies);
        var fileName = Path.GetFileName(selectedFilePath); // 文件名称
        var fileInfo = new FileInfo(selectedFilePath);
        var size = fileInfo.Length;
        var lastModified = fileInfo.LastWriteTimeUtc;
        var fileExtension = Path.GetExtension(fileName).TrimStart('.').ToLower(); // 文件后缀（如 "jpg"）
        var mimeType = GetMimeType(fileExtension);
        var data = new
        {
            path,
            size,
            name = fileName,
            policy_id = fileId,
            last_modified = GetLastModifiedTimestamp(lastModified),
            mime_type = mimeType
        };
        var updateJson = JsonSerializer.Serialize(data);
        var jsonContent = new StringContent(updateJson, Encoding.UTF8, "application/json");
        var response = await App.HttpClient.PutAsync(App.ServerUrl + "api/v3/file/upload", jsonContent);
        var responseString = await response.Content.ReadAsStringAsync();
        var json = JsonNode.Parse(responseString)!;
        return json;
    }

    private static string GetMimeType(string fileExtension)
    {
        return fileExtension.ToLower() switch
        {
            "svg" => "image/svg+xml",
            "jpg" or "jpeg" => "image/jpeg",
            "png" => "image/png",
            "gif" => "image/gif",
            "bmp" => "image/bmp",
            "webp" => "image/webp",
            "pdf" => "application/pdf",
            "txt" => "text/plain",
            "csv" => "text/csv",
            "json" => "application/json",
            "xml" => "application/xml",
            "mp3" => "audio/mpeg",
            "wav" => "audio/wav",
            "mp4" => "video/mp4",
            "avi" => "video/x-msvideo",
            "mpeg" => "video/mpeg",
            "zip" => "application/zip",
            "tar" => "application/x-tar",
            "gz" => "application/gzip",
            "iso" => "application/octet-stream",
            _ => "application/x-msdownload"
        };
    }

    private static long GetLastModifiedTimestamp(DateTime lastWrite)
    {
        var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return (long)(lastWrite - epoch).TotalMilliseconds;
    }
}