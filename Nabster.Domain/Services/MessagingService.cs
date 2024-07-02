using Nabster.Domain.Notifications;
using Nabster.Domain.Reports;

namespace Nabster.Domain.Services;

public class MessagingService(
                Account _account,
                Activity _activity,
                Funded _funded,
                AccountToSms _accountToSms,
                ActivityToSms _activityToSms,
                FundedToSms _fundedToSms,
                MessageToSms _messageToSms)
{
    public async Task ReplyToMessage(string message, string phoneNumber)
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
                _messageToSms.Notify($"Unrecognized command {command}", phoneNumber);
                break;
        }
    }

    public async Task ReplyAccount(string? budgetName, string accountName, string phoneNumbers)
    {
        var report = await _account.Generate(budgetName, accountName);
        _accountToSms.Notify(report, phoneNumbers);
    }

    public async Task ReplyActivity(string? budgetName, string categoryOrGroupName, string phoneNumbers)
    {
        var report = await _activity.Generate(budgetName, categoryOrGroupName);
        _activityToSms.Notify(report, phoneNumbers);

    }

    public async Task ReplyFunded(string? budgetName, string categoryOrGroupName, string phoneNumbers)
    {
        var report = await _funded.Generate(budgetName, categoryOrGroupName);
        _fundedToSms.Notify(report, phoneNumbers);

    }

    public void ReplyHelp(string phoneNumbers)
    {
        var message = "Commands:\n" +
                      "account <name>\n" +
                      "activity <category or group name>\n" +
                      "funded <category or group name>\n";
        _messageToSms.Notify(message, phoneNumbers);
    }

    public void ReplyMessage(string message, string phoneNumber)
    {
        _messageToSms.Notify(message, phoneNumber);
    }
}