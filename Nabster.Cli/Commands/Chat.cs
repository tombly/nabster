using System.CommandLine;
using Nabster.Cli.Commands.Chats;

namespace Nabster.Cli.Commands;

public static class Chat
{
    public static void AddChat(this Command parentCommand)
    {
        var command = new Command("chat", "Execute chat commands");

        command.AddAccount();
        command.AddActivity();
        command.AddFunded();

        parentCommand.Add(command);
    }
}