using System.Net.Http;

namespace CloudreveDesktop.utils;

public static class HttpUtil
{
    private static readonly HttpClient Client = App.HttpClient;
    // private static readonly HttpClient Client = new HttpClient() ; // 有需要可以修改成这个样子，但是注意要在下面代码中添加释放资源 

    // Delete请求，可以携带负载
    public static async Task<HttpResponseMessage> DeleteAsync(string requestUri, HttpContent? content)
    {
        var httpRequestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Delete,
            RequestUri = new Uri(requestUri),
            Content = content
        };
        return await Client.SendAsync(httpRequestMessage); // 可以修改为自己的Client
    }
}