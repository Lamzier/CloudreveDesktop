using System.Text.Json.Nodes;

namespace CloudreveDesktop.CloudreveApi;

public static class FilesApi
{
    // 获取文件夹内容
    public static async Task<JsonNode> Directory(string path)
    {
        var pathUri = Uri.EscapeDataString(path); // uri 编码
        var cookies = App.GetCookies();
        if (cookies.Length > 0) App.HttpClient.DefaultRequestHeaders.Add("cookie", cookies);
        var response = await App.HttpClient.GetAsync(App.ServerUrl + "api/v3/directory" + pathUri);
        var responseString = await response.Content.ReadAsStringAsync();
        var json = JsonNode.Parse(responseString)!;
        return json;
    }
}