using System.Text.Json.Nodes;

namespace Nabster.Server.Extensions;

internal static class JsonNodeExtensions
{
    /// <summary>
    /// Get an optional string value from a JSON object. Returns null if the key is
    /// missing or the value is empty.
    /// </summary>
    public static string? GetOptionalStringValue(this JsonNode node, string key)
    {
        if (node[key] == null || string.IsNullOrWhiteSpace(node[key]!.GetValue<string>()))
            return null;
        else
            return node[key]!.GetValue<string>();
    }

    /// <summary>
    /// Get an optional boolean value from a JSON object. Returns null if the key is missing.
    /// </summary>
    public static bool? GetOptionalBooleanValue(this JsonNode node, string key)
    {
        if (node[key] == null)
            return null;
        return node[key]!.GetValue<bool>();
    }

    /// <summary>
    /// Get an optional array of strings from a JSON object. Returns null if the key is
    /// missing or the array is empty.
    /// </summary>
    public static string[]? GetOptionalStringArrayValue(this JsonNode node, string key)
    {
        if (node[key] == null)
            return null;
        var value = node[key]!.AsArray().Select(n => n?.GetValue<string>() ?? string.Empty).ToArray();
        return value.Length == 0 ? null : value;
    }
}