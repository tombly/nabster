using System.CommandLine;
using System.Text;
using System.Text.Json;
using Nabster.Cli.Binders;
using Spectre.Console;

namespace Nabster.Cli.Commands.Chats;

public static class Activity
{
    public static void AddActivity(this Command parentCommand)
    {
        var command = new Command("activity", "Send an SMS with a category or group's current activity.");

        var budgetNameOption = new Option<string>(
            aliases: ["--budget-name", "-b"],
            description: "The name of the budget to generate the report for.");

        var categoryNameOption = new Option<string>(
            aliases: ["--category-name", "-c"],
            description: "The name of the category or group to generate the report for.");

        var phoneNumberOption = new Option<string>(
            aliases: ["--phone-number", "-p"],
            description: "The phone number to send the SMS to");

        command.AddOption(budgetNameOption);
        command.AddOption(categoryNameOption);
        command.AddOption(phoneNumberOption);

        command.SetHandler(async (budgetName, categoryName, phoneNumber, configFile, httpClient) =>
            {
                await AnsiConsole.Status().StartAsync("Generating", async ctx =>
                    {
                        using StringContent jsonContent = new(
                                JsonSerializer.Serialize(new
                                {
                                    budget = budgetName,
                                    category = categoryName,
                                    phone = phoneNumber
                                }),
                                Encoding.UTF8,
                                "application/json");

                        using var response = await httpClient.PostAsync(
                            "report/activity",
                            jsonContent);

                        response.EnsureSuccessStatusCode();
                    });
            },
            budgetNameOption,
            categoryNameOption,
            phoneNumberOption,
            ConfigFileOption.Value,
            new FunctionHttpClientBinder());

        parentCommand.AddCommand(command);
    }
}