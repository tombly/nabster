using System.CommandLine;

namespace Nabster.Cli.Commands;

internal sealed class UtilityCommand : Command
{
    public UtilityCommand(MemoCommand memoCommand)
        : base("utility", "Utility commands for managing YNAB data.")
    {
        Subcommands.Add(memoCommand);
    }
}
