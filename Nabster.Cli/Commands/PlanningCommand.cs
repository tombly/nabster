using System.CommandLine;
using Nabster.Cli.Binders;
using Nabster.Cli.Options;
using Nabster.Reports.Exporters;
using Spectre.Console;

namespace Nabster.Cli.Commands;

public static class PlanningCommand
{
    public static void AddPlanning(this Command parentCommand)
    {
        var command = new Command("planning", "Generate a planning report for a budget.");

        var budgetNameOption = new Option<string>(
            aliases: ["--budget-name", "-b"],
            description: "The name of the budget to generate the report for.");

        var outputFormatOption = new Option<string>(
            aliases: ["--output-format", "-o"],
            description: "The output file type, xlsx or html (default).");

        command.AddOption(budgetNameOption);
        command.AddOption(outputFormatOption);

        command.SetHandler(async (budgetName, outputFormat, configFile, ynabClient) =>
            {
                await AnsiConsole.Status().StartAsync("Generating", async ctx =>
                    {
                        var report = await Reports.Generators.Planning.Generate(budgetName, ynabClient);

                        byte[] fileBytes;
                        string fileExtension;
                        switch (outputFormat)
                        {
                            case "xlsx":
                                fileBytes = PlanningToExcel.Create(report);
                                fileExtension = "xlsx";
                                break;
                            default:
                                fileBytes = PlanningToHtml.Create(report);
                                fileExtension = "html";
                                break;
                        }
                        var fileName = $"{budgetName} Planning {DateTime.Now:yyyyMMdd}.{fileExtension}";
                        var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);
                        using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
                        await stream.WriteAsync(fileBytes);
                    });
            },
            budgetNameOption, outputFormatOption, ConfigFileOption.Value, new YnabClientBinder());

        parentCommand.AddCommand(command);
    }
}