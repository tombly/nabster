using System.CommandLine;
using Nabster.Cli.Commands;

namespace Nabster.Cli;

public static class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Nabster is a tool for YNAB reporting.");

        rootCommand.AddGlobalOption(ConfigFileOption.Value);
        rootCommand.AddChat();
        rootCommand.AddPerformance();
        rootCommand.AddPlanning();
        rootCommand.AddSpend();

        return await rootCommand.InvokeAsync(args);
    }
}