using System.CommandLine;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Nabster.Cli.Config;
using Spectre.Console;

namespace Nabster.Cli.Commands;

internal sealed class FuncCommand : Command
{
    public FuncCommand(IAnsiConsole ansiConsole, IOptions<FunctionOptions> options)
        : base("func", "Chat with Nabster.")
    {
        Options.Add(new Option<string>("--message") { Description = "The message to send." });
        Options.Add(new Option<string>("--from") { Description = "The phone number of the sender." });

        SetAction(async parseResult =>
        {
            var functionAppUrl = options.Value.Url ?? throw new InvalidOperationException("Function App Url not found.");
            var functionAppKey = options.Value.Key ?? throw new InvalidOperationException("Function App Key not found.");

            var httpClient = new HttpClient { BaseAddress = new Uri(functionAppUrl) };
            httpClient.DefaultRequestHeaders.Add("x-functions-key", functionAppKey);

            var message = parseResult.GetValue<string>("--message");
            var from = parseResult.GetValue<string>("--from");

            await ansiConsole.Status().StartAsync("Sending message...", async ctx =>
                {
                    using StringContent jsonContent = new(
                            JsonSerializer.Serialize(new { message, from }),
                            Encoding.UTF8,
                            "application/json");

                    using var response = await httpClient.PostAsync(
                            "IncomingMessage",
                            jsonContent);

                    response.EnsureSuccessStatusCode();

                    ansiConsole.WriteLine(await response.Content.ReadAsStringAsync());
                });
        });
    }
}