using System.CommandLine;
using System.Text;
using System.Text.Json;
using Spectre.Console;

namespace Nabster.Cli.Commands;

public static class Performance
{
    public static void AddPerformance(this RootCommand rootCommand)
    {
        var command = new Command("performance", "Generate a performance report for a budget.");

        var budgetNameOption = new Option<string>(
            aliases: ["--budget-name", "-b"],
            description: "The name of the budget to generate the report for.");

        command.AddOption(budgetNameOption);

        command.SetHandler(async (budgetName, configFile, httpClient) =>
            {
                await AnsiConsole.Status().StartAsync("Generating", async ctx =>
                    {
                        using StringContent jsonContent = new(
                                JsonSerializer.Serialize(new
                                {
                                    budget = budgetName
                                }),
                                Encoding.UTF8,
                                "application/json");

                        using var response = await httpClient.PostAsync(
                              "report/performance",
                              jsonContent);

                        response.EnsureSuccessStatusCode();

                        var file = await response.Content.ReadAsByteArrayAsync();

                        var contentType = response.Content.Headers.ContentType!.MediaType;
                        if (contentType == "application/html")
                        {
                            var fileName = $"{budgetName} Performance {DateTime.Now:yyyyMMdd}.html";
                            var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);
                            using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
                            await stream.WriteAsync(file);
                        }
                        else if (contentType == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
                        {
                            var fileName = $"{budgetName} Performance {DateTime.Now:yyyyMMdd}.xlsx";
                            var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);
                            using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
                            await stream.WriteAsync(file);
                        }
                        else
                        {
                            throw new InvalidOperationException($"Unexpected content type: {contentType}");
                        }
                    });
            },
            budgetNameOption,
            ConfigFileOption.Value,
            new FunctionHttpClientBinder());

        rootCommand.AddCommand(command);
    }
}