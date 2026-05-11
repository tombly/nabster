using System.CommandLine;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Nabster.Cli.Config;
using Nabster.Reporting.Reports.Daily;
using Spectre.Console;

namespace Nabster.Cli.Commands;

internal sealed class DailyCommand : Command
{
    public DailyCommand(IAnsiConsole ansiConsole, IOptions<ServerOptions> options, DailyReport dailyReport)
        : base("daily", "Creates a daily executive summary report.")
    {
        Options.Add(new Option<bool>("--demo") { Description = "Generate a demo report." });
        Options.Add(new Option<string>("--budget-name") { Description = "The name of the budget to generate the report for." });
        Options.Add(new Option<string>("--url") { Description = "The URL of the function app." });
        Options.Add(new Option<string[]>("--email-addresses") { Description = "The email addresses to send the report to.", AllowMultipleArgumentsPerToken = true });
        Options.Add(new Option<string[]>("--category-names") { Description = "The category names to include in the report. If omitted, all categories are included.", AllowMultipleArgumentsPerToken = true });

        SetAction(async parseResult =>
        {
            await ansiConsole.Status().StartAsync("Building report", async ctx =>
            {
                var budgetName = parseResult.GetValue<string>("--budget-name");
                var isDemo = parseResult.GetValue<bool>("--demo");
                var categoryNames = parseResult.GetValue<string[]>("--category-names");
                var emailAddresses = parseResult.GetValue<string[]>("--email-addresses");
                var url = parseResult.GetValue<string>("--url");

                if (url == null)
                {
                    var report = await dailyReport.Build(budgetName, isDemo, categoryNames);
                    ansiConsole.WriteLine(report);
                }
                else
                {
                    var functionAppKey = options.Value.FunctionAppKey;
                    if (string.IsNullOrEmpty(functionAppKey))
                        throw new InvalidOperationException("Function App Key not found.");

                    using var httpClient = new HttpClient();
                    httpClient.DefaultRequestHeaders.Add("x-functions-key", functionAppKey);

                    using StringContent jsonContent = new(
                            JsonSerializer.Serialize(new { budgetName, isDemo, categoryNames, emailAddresses }),
                            Encoding.UTF8,
                            "application/json");

                    using var response = await httpClient.PostAsync(url, jsonContent);

                    response.EnsureSuccessStatusCode();

                    ansiConsole.WriteLine(await response.Content.ReadAsStringAsync());
                }
            });
        });
    }
}