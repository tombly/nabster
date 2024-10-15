using System.CommandLine;
using System.Text;
using System.Text.Json;
using Nabster.Cli.Binders;
using Spectre.Console;

namespace Nabster.Cli.Commands;

public static class Chat
{
    public static void AddChat(this Command parentCommand)
    {
        var command = new Command("chat", "Chat with Nabster.");

        var bodyOption = new Option<string>(
            aliases: ["--body", "-b"],
            description: "The message to send.");
        
        var fromOption = new Option<string>(
            aliases: ["--from", "-f"],
            description: "The phone number of the sender.");
 
        command.AddOption(bodyOption);
        command.AddOption(fromOption);

        command.SetHandler(async (body, from, configFile, httpClient) =>
            {
                await AnsiConsole.Status().StartAsync("Sending message...", async ctx =>
                    {
                        using StringContent jsonContent = new(
                                JsonSerializer.Serialize(new
                                {
                                    body,
                                    from
                                }),
                                Encoding.UTF8,
                                "application/json");

                        using var response = await httpClient.PostAsync(
                            "IncomingMessage",
                            jsonContent);

                        response.EnsureSuccessStatusCode();
                    });
            },
            bodyOption,
            fromOption,
            ConfigFileOption.Value,
            new FunctionHttpClientBinder());

        parentCommand.AddCommand(command);
    }
}