using System.Text.Json.Nodes;

namespace CloudreveDesktop.utils;

public static class ResultUtil
{
    public static JsonNode GetErrorJson()
    {
        var jsonObject = new JsonObject
        {
            { "code", -1 },
            { "msg", "Error" }
        };
        return jsonObject;
    }

    public static JsonNode GetJson(int code, string msg)
    {
        var jsonObject = new JsonObject
        {
            { "code", code },
            { "msg", msg }
        };
        return jsonObject;
    }
}