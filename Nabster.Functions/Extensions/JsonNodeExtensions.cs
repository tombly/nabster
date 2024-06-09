using System.Text.Json.Nodes;

namespace Nabster.Functions.Extensions;

public static class JsonNodeExtensions
{
  public static string GetStringValue(this JsonNode node, string key)
  {
    return (node[key] ?? throw new Exception($"Json missing {key}")).GetValue<string>();
  }

  public static List<string> GetStringListValue(this JsonNode node, string key)
  {
    return (node[key] ?? throw new Exception($"Json missing {key}")).AsArray().Select(n => n?.GetValue<string>() ?? string.Empty).ToList();
  }
}