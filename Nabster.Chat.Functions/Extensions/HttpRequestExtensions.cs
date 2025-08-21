using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;

namespace Nabster.Chat.Functions.Extensions;

internal static class HttpRequestExtensions
{
    public static async Task<JsonNode> AsJsonNode(this HttpRequest request)
    {
        using var reader = new StreamReader(request.Body);
        return JsonSerializer.Deserialize<JsonNode>(await reader.ReadToEndAsync()) ?? throw new Exception("Failed to parse request");
    }
}