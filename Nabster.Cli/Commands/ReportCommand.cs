using System.CommandLine;

namespace Nabster.Cli.Commands;

public static class ReportCommand
{
    public static void AddReports(this Command parentCommand)
    {
        var command = new Command("report", "Generates a finance report.");

        command.AddPerformance();
        command.AddPlanning();
        command.AddSpend();

        parentCommand.AddCommand(command);
    }
}