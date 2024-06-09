using System.CommandLine;
using System.Text;
using System.Text.Json;
using Spectre.Console;

namespace Nabster.Cli;

public static class Spend
{
    public static void AddSpend(this RootCommand rootCommand)
    {
        var command = new Command("spend", "Generate a spend report for a budget category.");

        var budgetNameOption = new Option<string>(
            aliases: ["--budget-name", "-b"],
            description: "The name of the budget to generate the report for.");

        var categoryNameOption = new Option<string>(
            aliases: ["--category-name", "-c"],
            description: "The name of the category to generate the report for.");

        var yearMonthOption = new Option<string>(
            aliases: ["--month", "-m"],
            description: "The year and month to generate the report for, e.g.: 2024-05");

        command.AddOption(budgetNameOption);
        command.AddOption(categoryNameOption);
        command.AddOption(yearMonthOption);

        command.SetHandler(async (budgetName, categoryName, yearMonth, configFile, httpClient) =>
            {
                await AnsiConsole.Status().StartAsync("Generating", async ctx =>
                    {
                        using StringContent jsonContent = new(
                                JsonSerializer.Serialize(new
                                {
                                    budget = budgetName,
                                    category = categoryName,
                                    month = yearMonth
                                }),
                                Encoding.UTF8,
                                "application/json");

                        using var response = await httpClient.PostAsync(
                              "report/spend",
                              jsonContent);

                        response.EnsureSuccessStatusCode();

                        var file = await response.Content.ReadAsByteArrayAsync();

                        var contentType = response.Content.Headers.ContentType!.MediaType;
                        if (contentType == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
                        {
                            var fileName = $"{budgetName} Spend {DateTime.Now:yyyyMMdd}.xlsx";
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
            categoryNameOption,
            yearMonthOption,
            ConfigFileOption.Value,
            new FunctionHttpClientBinder());

        rootCommand.AddCommand(command);
    }
}