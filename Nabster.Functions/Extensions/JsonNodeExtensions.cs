using System.Text.Json.Nodes;

namespace Nabster.Functions.Extensions;

public static class JsonNodeExtensions
{
  /// <summary>
  /// Get a required string value from a JSON object. Throws an exception if
  /// the key is missing or the value is empty.
  /// </summary>
  public static string GetRequiredStringValue(this JsonNode node, string key)
  {
    var value = (node[key] ?? throw new Exception($"JSON missing {key}")).GetValue<string>();
    if (string.IsNullOrWhiteSpace(value))
      throw new Exception($"JSON property {key} has empty value");
    return value;
  }

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
  /// Get a required list of strings from a JSON object. Throws an exception if
  /// the key is missing or the list is empty.
  /// </summary>
  public static List<string> GetRequiredStringListValue(this JsonNode node, string key)
  {
    var value = (node[key] ?? throw new Exception($"Json missing {key}")).AsArray().Select(n => n?.GetValue<string>() ?? string.Empty).ToList();
    if (value.Count == 0)
      throw new Exception($"JSON property {key} has empty list");
    return value;
  }
}