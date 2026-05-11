namespace Nabster.Server.Config;

/// <summary>
/// Describes the configuration options required by the Server feature.
/// </summary>
public class ServerOptions
{
    public const string Section = "Server";

    public string Smtp2GoApiKey { get; set; } = string.Empty;
    public string Smtp2GoEmailAddress { get; set; } = string.Empty;
}