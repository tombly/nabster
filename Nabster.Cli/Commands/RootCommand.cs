using BaseRootCommand = System.CommandLine.RootCommand;

namespace Nabster.Cli.Commands;

internal sealed class RootCommand : BaseRootCommand
{
    public RootCommand(ReportCommand reportCommand, UtilityCommand utilityCommand)
        : base("Nabster is a tool for YNAB reports.")
    {
        ArgumentNullException.ThrowIfNull(reportCommand);
        ArgumentNullException.ThrowIfNull(utilityCommand);

        Subcommands.Add(reportCommand);
        Subcommands.Add(utilityCommand);
    }
}