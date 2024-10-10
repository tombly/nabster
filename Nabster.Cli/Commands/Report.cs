using System.CommandLine;
using Nabster.Cli.Commands.Reports;

namespace Nabster.Cli.Commands;

public static class Report
{
    public static void AddReport(this Command parentCommand)
    {
        var command = new Command("report", "Execute report commands");

        command.AddPerformance();
        command.AddPlanning();
        command.AddSpend();

        parentCommand.Add(command);
    }
}