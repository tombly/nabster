using System.CommandLine;

namespace Nabster.Cli.Commands;

public static class ChatCommand
{
    public static void AddChat(this Command parentCommand)
    {
        var command = new Command("chat", "Chat with Nabster.");

        command.AddDirect();
        command.AddFunc();

        parentCommand.AddCommand(command);
    }
}