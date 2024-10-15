using System.Text;
using Microsoft.Extensions.Logging;
using Nabster.Domain.Reports;

namespace Nabster.Domain.Services;

public class MessagingService(Account _account, Activity _activity, Funded _funded, SmsService _smsService)
{
    public async Task ReplyToMessage(string message, string phoneNumber, ILogger logger)
    {
        logger.LogInformation("Received message from {phoneNumber}: {message}", phoneNumber, message);

        try
        {
            var command = message.Split(' ')[0].ToLowerInvariant();
            var argument = message[(message.IndexOf(' ') + 1)..].Trim().ToLowerInvariant();
            var response = command switch
            {
                "account" => await ReplyAccount(null, argument),
                "activity" => await ReplyActivity(null, argument),
                "funded" => await ReplyFunded(null, argument),
                "commands" => ReplyHelp(),
                _ => $"Unrecognized command {command}",
            };
            logger.LogInformation("Replied to message {message} with {response}", message, response);
            _smsService.Send(phoneNumber, response);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error processing message {message}", message);
            _smsService.Send("Hm, that didn't work", phoneNumber);
        }
    }

    public async Task<string> ReplyAccount(string? budgetName, string accountName)
    {
        var report = await _account.Generate(budgetName, accountName);

        var message = new StringBuilder();

        foreach (var balance in report.Balances)
            message.AppendLine($"{balance.Name}: {balance.Balance:c0}");

        if (report.Balances.Count > 1)
            message.AppendLine($"Total: {report.Balances.Sum(b => b.Balance):c0}");

        return message.ToString();
    }

    public async Task<string> ReplyActivity(string? budgetName, string categoryOrGroupName)
    {
        var report = await _activity.Generate(budgetName, categoryOrGroupName);
        return $"{report.Name} activity: {report.Activity:c0} of {report.Need:c0} ({report.Activity / report.Need:p0})";
    }

    public async Task<string> ReplyFunded(string? budgetName, string categoryOrGroupName)
    {
        var report = await _funded.Generate(budgetName, categoryOrGroupName);
        return report.Categories.Count == 1 ?
                                    BuildMessageForCategory(report) :
                                    BuildMessageForCategories(report);
    }

    private static string BuildMessageForCategory(FundedReport report)
    {
        return $"Funded {report.Name}: {report.Categories[0].Funded:c0} of {report.Categories[0].Target:c0} ({report.Categories[0].Funded / report.Categories[0].Target:p0})";
    }

    private static string BuildMessageForCategories(FundedReport report)
    {
        var message = new StringBuilder();

        foreach (var category in report.Categories)
        {
            var funded = category.Funded;
            var target = category.Target;
            var percentage = funded / target;

            message.AppendLine($"{category.Name}: ({percentage:p0})");
        }

        return message.ToString();
    }

    public static string ReplyHelp()
    {
        return "Commands:\n" +
               "account <name>\n" +
               "activity <category or group name>\n" +
               "funded <category or group name>\n";
    }
}