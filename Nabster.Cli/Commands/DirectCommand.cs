using System.CommandLine;
using Microsoft.Extensions.Logging;
using Nabster.Chat;
using Nabster.Cli.Binders;
using Spectre.Console;

namespace Nabster.Cli.Commands;

public static class Direct
{
    public static void AddDirect(this Command parentCommand)
    {
        var command = new Command("direct", "Chat with Nabster.");

        var messageOption = new Option<string>(
            aliases: ["--message", "-m"],
            description: "The message to send.");

        command.AddOption(messageOption);

        command.SetHandler(async (message, ynabApiClient, chatCompletionService, smsClient) =>
        {
            await AnsiConsole.Status().StartAsync("Sending message...", async ctx =>
            {
                using var factory = LoggerFactory.Create(builder => builder.AddConsole());
                var logger = factory.CreateLogger("Program");

                var chatService = new ChatService(chatCompletionService, ynabApiClient, smsClient);
                var response = await chatService.Reply(message, logger);
                Console.WriteLine(response);
            });
        },
        messageOption,
        new YnabClientBinder(),
        new ChatCompletionServiceBinder(),
        new SmsClientBinder());
        parentCommand.AddCommand(command);
    }
}