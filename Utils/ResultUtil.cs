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
}