using System.Text.Json.Nodes;

namespace CloudreveDesktop.CloudreveApi;

public static class UserApi
{
    // 获取用户设置信息
    public static async Task<JsonNode> GetUserSetting()
    {
        var cookies = App.GetCookies();
        if (cookies.Length > 0) App.HttpClient.DefaultRequestHeaders.Add("cookie", cookies);
        var response = await App.HttpClient.GetAsync(App.ServerUrl + "api/v3/user/setting");
        var responseString = await response.Content.ReadAsStringAsync();
        var json = JsonNode.Parse(responseString)!;
        return json;
    }
}