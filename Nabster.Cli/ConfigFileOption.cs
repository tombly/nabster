using System.CommandLine;

namespace Nabster.Cli;

public static class ConfigFileOption
{
    public static Option<string> Value { get; } = CreateConfigFileOption();

    private static Option<string> CreateConfigFileOption()
    {
        return new Option<string>(
            aliases: ["--config-file"],
            description: "The configuration file to use.",
            getDefaultValue: () => "config.json");
    }
}
