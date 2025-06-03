using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using CloudreveDesktop.utils;

namespace CloudreveDesktop.CloudreveApi;

public static class UserApi
{
    // 获取用户设置信息
    public static async Task<JsonNode> GetUserSetting()
    {
        try
        {
            var cookies = App.GetCookies();
            if (cookies.Length <= 0) return ResultUtil.GetErrorJson();
            App.HttpClient.DefaultRequestHeaders.Add("cookie", cookies);
            var response = await App.HttpClient.GetAsync(App.ServerUrl + "api/v3/user/setting");
            var responseString = await response.Content.ReadAsStringAsync();
            var json = JsonNode.Parse(responseString)!;
            return json;
        }
        catch (Exception)
        {
            return ResultUtil.GetErrorJson();
        }
    }

    // 获取用户存储容量
    public static async Task<JsonNode> GetUserStorage()
    {
        var cookies = App.GetCookies();
        if (cookies.Length <= 0) return ResultUtil.GetErrorJson();
        App.HttpClient.DefaultRequestHeaders.Add("cookie", cookies);
        var response = await App.HttpClient.GetAsync(App.ServerUrl + "api/v3/user/storage");
        var responseString = await response.Content.ReadAsStringAsync();
        var json = JsonNode.Parse(responseString)!;
        return json;
    }

    public static async Task<JsonNode> Login(string username, string password, string captchaCode)
    {
        try
        {
            var loginData = new
            {
                Password = password,
                userName = username,
                captchaCode
            };
            var loginJson = JsonSerializer.Serialize(loginData);
            var jsonContent = new StringContent(loginJson, Encoding.UTF8, "application/json");
            var response = await App.HttpClient.PostAsync(App.ServerUrl + "api/v3/user/session", jsonContent);
            var responseString = await response.Content.ReadAsStringAsync();
            var json = JsonNode.Parse(responseString)!;
            var cookies = response.Headers.GetValues("Set-Cookie").ToList();
            App.Cookies.Clear(); //清除cookies
            App.Cookies.AddRange(cookies); // 添加cookies
            return json;
        }
        catch (Exception)
        {
            return ResultUtil.GetJson(1231, "目标服务器连接失败");
        }
    }
}