using System.CommandLine;

namespace Nabster.Cli.Commands;

internal sealed class ChatCommand : Command
{
    public ChatCommand(DirectCommand directCommand, FuncCommand funcCommand)
        : base("chat", "Chat with Nabster.")
    {
        Subcommands.Add(directCommand);
        Subcommands.Add(funcCommand);
    }
}