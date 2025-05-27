using System.IO;
using System.Net.Http;
using System.Text.Json.Nodes;

namespace CloudreveDesktop.CloudreveApi;

public static class FilesApi
{
    // 获取文件夹内容
    public static async Task<JsonNode> GetDirectory(string path)
    {
        var pathUri = Uri.EscapeDataString(path); // uri 编码
        var cookies = App.GetCookies();
        if (cookies.Length > 0) App.HttpClient.DefaultRequestHeaders.Add("cookie", cookies);
        var response = await App.HttpClient.GetAsync(App.ServerUrl + "api/v3/directory" + pathUri);
        var responseString = await response.Content.ReadAsStringAsync();
        var json = JsonNode.Parse(responseString)!;
        return json;
    }

    public static async Task<JsonNode> GetDownloadKey(string fileId)
    {
        var cookies = App.GetCookies();
        if (cookies.Length > 0) App.HttpClient.DefaultRequestHeaders.Add("cookie", cookies);
        var response = await App.HttpClient.PutAsync(App.ServerUrl + "api/v3/file/download/" + fileId, null);
        var responseString = await response.Content.ReadAsStringAsync();
        var json = JsonNode.Parse(responseString)!;
        return json;
    }

    public static async Task<bool> DownloadFile(string url, string savePath)
    {
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
}