using System.CommandLine;
using Nabster.Cli.Binders;
using Nabster.Cli.Options;
using Nabster.Reports.Exporters;
using Spectre.Console;

namespace Nabster.Cli.Commands;

public static class SpendCommand
{
    public static void AddSpend(this Command parentCommand)
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

        var outputFormatOption = new Option<string>(
             aliases: ["--output-format", "-o"],
             description: "The output file type, xlsx or html (default).");

        command.AddOption(budgetNameOption);
        command.AddOption(categoryNameOption);
        command.AddOption(yearMonthOption);
        command.AddOption(outputFormatOption);

        command.SetHandler(async (budgetName, categoryName, yearMonth, outputFormat, configFile, ynabClient) =>
            {
                await AnsiConsole.Status().StartAsync("Generating", async ctx =>
                    {
                        var report = await Reports.Generators.Spend.Generate(budgetName, categoryName, yearMonth, ynabClient);

                        byte[] fileBytes;
                        string fileExtension;
                        switch (outputFormat)
                        {
                            case "xlsx":
                                fileBytes = SpendToExcel.Create(report);
                                fileExtension = "xlsx";
                                break;
                            default:
                                fileBytes = SpendToHtml.Create(report);
                                fileExtension = "html";
                                break;
                        }
                        var fileName = $"{budgetName} Spend {yearMonth}.{fileExtension}";
                        var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);
                        using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
                        await stream.WriteAsync(fileBytes);
                    });
            },
            budgetNameOption, categoryNameOption, yearMonthOption, outputFormatOption, ConfigFileOption.Value, new YnabClientBinder());

        parentCommand.AddCommand(command);
    }
}