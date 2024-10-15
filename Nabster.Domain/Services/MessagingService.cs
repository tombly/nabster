using System.Text;
using Nabster.Domain.Reports;

namespace Nabster.Domain.Services;

public class MessagingService(Account _account, Activity _activity, Funded _funded, SmsService _smsService)
{
    public async Task ReplyToMessage(string message, string phoneNumber)
    {
        try
        {
            var command = message.Split(' ')[0].ToLowerInvariant();
            var parameter = message[(message.IndexOf(' ') + 1)..].Trim().ToLowerInvariant();

            switch (command)
            {
                case "account":
                    await ReplyAccount(null, parameter, phoneNumber);
                    break;
                case "activity":
                    await ReplyActivity(null, parameter, phoneNumber);
                    break;
                case "funded":
                    await ReplyFunded(null, parameter, phoneNumber);
                    break;
                case "commands":
                    ReplyHelp(phoneNumber);
                    break;
                default:
                    ReplyMessage($"Unrecognized command {command}", phoneNumber);
                    break;
            }
        }
        catch(Exception exception)
        {
            ReplyMessage(exception.Message, phoneNumber);
        }
    }

    public async Task ReplyAccount(string? budgetName, string accountName, string phoneNumbers)
    {
        var report = await _account.Generate(budgetName, accountName);

        var message = new StringBuilder();

        foreach (var balance in report.Balances)
            message.AppendLine($"{balance.Name}: {balance.Balance:c0}");

        if (report.Balances.Count > 1)
            message.AppendLine($"Total: {report.Balances.Sum(b => b.Balance):c0}");

        _smsService.Send(phoneNumbers, message.ToString());
    }

    public async Task ReplyActivity(string? budgetName, string categoryOrGroupName, string phoneNumbers)
    {
        var report = await _activity.Generate(budgetName, categoryOrGroupName);
        var message = $"{report.Name} activity: {report.Activity:c0} of {report.Need:c0} ({report.Activity / report.Need:p0})";
        _smsService.Send(phoneNumbers, message);
    }

    public async Task ReplyFunded(string? budgetName, string categoryOrGroupName, string phoneNumbers)
    {
        var report = await _funded.Generate(budgetName, categoryOrGroupName);
        _smsService.Send(phoneNumbers, report.Categories.Count == 1 ?
                                  BuildMessageForCategory(report) :
                                  BuildMessageForCategories(report));
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

    public void ReplyHelp(string phoneNumbers)
    {
        var message = "Commands:\n" +
                      "account <name>\n" +
                      "activity <category or group name>\n" +
                      "funded <category or group name>\n";
        _smsService.Send(phoneNumbers, message);
    }

    public void ReplyMessage(string message, string phoneNumber)
    {
        _smsService.Send(phoneNumber, message);
    }
}