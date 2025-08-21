using System.CommandLine;
using Microsoft.Extensions.Logging;
using Nabster.Chat.Services;
using Spectre.Console;

namespace Nabster.Cli.Commands;

internal sealed class DirectCommand : Command
{
    public DirectCommand(IAnsiConsole ansiConsole, ChatService chatService, ILogger logger)
        : base("direct", "Chat with Nabster.")
    {
        var messageOption = new Option<string>("--message") { Description = "The message to send." };
        Options.Add(messageOption);

        SetAction(async parseResult =>
        {
            await ansiConsole.Status().StartAsync("Sending message...", async ctx =>
            {
                var message = parseResult.GetValue(messageOption);
                if (message != null)
                {
                    var response = await chatService.Reply(message, logger);
                    ansiConsole.WriteLine(response);
                }
            });
        });
    }
}