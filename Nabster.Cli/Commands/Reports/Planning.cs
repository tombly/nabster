using System.CommandLine;
using Nabster.Cli.Binders;
using Spectre.Console;

namespace Nabster.Cli.Commands.Reports;

public static class Planning
{
    public static void AddPlanning(this Command parentCommand)
    {
        var command = new Command("planning", "Generate a planning report for a budget.");

        var budgetNameOption = new Option<string>(
            aliases: ["--budget-name", "-b"],
            description: "The name of the budget to generate the report for.");

        command.AddOption(budgetNameOption);

        command.SetHandler(async (budgetName, configFile, ynabClient) =>
            {
                await AnsiConsole.Status().StartAsync("Generating", async ctx =>
                    {
                        var report = await Domain.Reports.Planning.Generate(budgetName, ynabClient);

                        var fileBytes = Domain.Exports.PlanningToExcel.Create(report);
                        var fileExtension = "xlsx";

                        var fileName = $"{budgetName} Planning {DateTime.Now:yyyyMMdd}.{fileExtension}";
                        var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);
                        using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
                        await stream.WriteAsync(fileBytes);
                    });
            },
            budgetNameOption, ConfigFileOption.Value, new YnabClientBinder());

        parentCommand.AddCommand(command);
    }
}