using BaseRootCommand = System.CommandLine.RootCommand;

namespace Nabster.Cli.Commands;

internal sealed class RootCommand : BaseRootCommand
{
    public RootCommand(ChatCommand chatCommand, ReportCommand reportCommand)
        : base("Nabster is a tool for YNAB chat and reports.")
    {
        ArgumentNullException.ThrowIfNull(chatCommand);
        ArgumentNullException.ThrowIfNull(reportCommand);

        Subcommands.Add(chatCommand);
        Subcommands.Add(reportCommand);
    }
}