namespace Nabster.Cli.Config;

/// <summary>
/// Describes the configuration options required by the Function command.
/// </summary>
public class FunctionOptions
{
    public const string Section = "Function";

    public string Url { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
}