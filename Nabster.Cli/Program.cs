using System.CommandLine;
using Nabster.Cli.Commands;
using Nabster.Cli.Options;

namespace Nabster.Cli;

public static class Program
{
    static async Task<int> Main(string[] args)
    {
        var command = new RootCommand("Nabster is a tool for YNAB reporting.");

        command.AddGlobalOption(ConfigFileOption.Value);
        command.AddChat();
        command.AddReports();

        return await command.InvokeAsync(args);
    }
}