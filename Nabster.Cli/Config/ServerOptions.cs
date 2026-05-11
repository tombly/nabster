namespace Nabster.Cli.Config;

/// <summary>
/// Describes the configuration options required by the Server feature.
/// </summary>
public class ServerOptions
{
    public const string Section = "Server";

    public string FunctionAppKey { get; set; } = string.Empty;
}