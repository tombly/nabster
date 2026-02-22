using BaseRootCommand = System.CommandLine.RootCommand;

namespace Nabster.Cli.Commands;

internal sealed class RootCommand : BaseRootCommand
{
    public RootCommand(ChatCommand chatCommand, ReportCommand reportCommand, UtilityCommand utilityCommand)
        : base("Nabster is a tool for YNAB chat and reports.")
    {
        ArgumentNullException.ThrowIfNull(chatCommand);
        ArgumentNullException.ThrowIfNull(reportCommand);
        ArgumentNullException.ThrowIfNull(utilityCommand);

        Subcommands.Add(chatCommand);
        Subcommands.Add(reportCommand);
        Subcommands.Add(utilityCommand);
    }
}