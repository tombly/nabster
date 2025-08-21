using System.CommandLine;

namespace Nabster.Cli.Commands;

internal sealed class ReportCommand : Command
{
    public ReportCommand(HistoricalCommand performanceCommand, PlanningCommand planningCommand, SpendCommand spendCommand)
        : base("report", "Generates a finance report.")
    {
        Subcommands.Add(performanceCommand);
        Subcommands.Add(planningCommand);
        Subcommands.Add(spendCommand);
    }
}