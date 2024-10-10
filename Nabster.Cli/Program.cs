using System.CommandLine;
using Nabster.Cli.Commands;

namespace Nabster.Cli;

public static class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Nabster is a tool for generating YNAB reports.");

        rootCommand.AddGlobalOption(ConfigFileOption.Value);
        rootCommand.AddChat();
        rootCommand.AddReport();

        return await rootCommand.InvokeAsync(args);
    }
}