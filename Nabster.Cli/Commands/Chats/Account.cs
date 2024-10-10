using System.CommandLine;
using System.Text;
using System.Text.Json;
using Nabster.Cli.Binders;
using Spectre.Console;

namespace Nabster.Cli.Commands.Chats;

public static class Account
{
    public static void AddAccount(this Command parentCommand)
    {
        var command = new Command("account", "Send an SMS with an account's balance.");

        var budgetNameOption = new Option<string>(
            aliases: ["--budget-name", "-b"],
            description: "The name of the budget to generate the report for.");

        var accountNameOption = new Option<string>(
            aliases: ["--account-name", "-c"],
            description: "The name of the account to generate the report for.");

        var phoneNumberOption = new Option<string>(
            aliases: ["--phone-number", "-p"],
            description: "The phone number to send the SMS to");

        command.AddOption(budgetNameOption);
        command.AddOption(accountNameOption);
        command.AddOption(phoneNumberOption);

        command.SetHandler(async (budgetName, accountName, phoneNumber, configFile, httpClient) =>
            {
                await AnsiConsole.Status().StartAsync("Generating", async ctx =>
                    {
                        using StringContent jsonContent = new(
                                JsonSerializer.Serialize(new
                                {
                                    budget = budgetName,
                                    account = accountName,
                                    phone = phoneNumber
                                }),
                                Encoding.UTF8,
                                "application/json");

                        using var response = await httpClient.PostAsync(
                            "report/account",
                            jsonContent);

                        response.EnsureSuccessStatusCode();
                    });
            },
            budgetNameOption,
            accountNameOption,
            phoneNumberOption,
            ConfigFileOption.Value,
            new FunctionHttpClientBinder());

        parentCommand.AddCommand(command);
    }
}