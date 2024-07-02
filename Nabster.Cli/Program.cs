using System.CommandLine;

namespace Nabster.Cli;

public static class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("nabster is a tool for generating YNAB reports.");

        rootCommand.AddGlobalOption(ConfigFileOption.Value);
        rootCommand.AddAccount();
        rootCommand.AddActivity();
        rootCommand.AddFunded();
        rootCommand.AddPerformance();
        rootCommand.AddPlanning();
        rootCommand.AddSpend();

        return await rootCommand.InvokeAsync(args);
    }
}