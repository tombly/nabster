using System.CommandLine;
using System.Text;
using System.Text.Json;
using Nabster.Cli.Binders;
using Nabster.Cli.Options;
using Spectre.Console;

namespace Nabster.Cli.Commands;

public static class FuncCommand
{
    public static void AddFunc(this Command parentCommand)
    {
        var command = new Command("func", "Chat with Nabster.");

        var messageOption = new Option<string>(
            aliases: ["--message", "-m"],
            description: "The message to send.");

        var fromOption = new Option<string>(
            aliases: ["--from", "-f"],
            description: "The phone number of the sender.");

        command.AddOption(messageOption);
        command.AddOption(fromOption);

        command.SetHandler(async (message, from, configFile, httpClient) =>
            {
                await AnsiConsole.Status().StartAsync("Sending message...", async ctx =>
                    {
                            using StringContent jsonContent = new(
                                JsonSerializer.Serialize(new { message, from }),
                                Encoding.UTF8,
                                "application/json");

                            using var response = await httpClient.PostAsync(
                                "IncomingMessage",
                                jsonContent);

                            response.EnsureSuccessStatusCode();

                            Console.WriteLine(await response.Content.ReadAsStringAsync());
                    });
            },
            messageOption,
            fromOption,
            ConfigFileOption.Value,
            new FunctionHttpClientBinder());

        parentCommand.AddCommand(command);
    }
}