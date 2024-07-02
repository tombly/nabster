using System.Text;
using Nabster.Domain.Reports;
using Nabster.Domain.Services;

namespace Nabster.Domain.Notifications;

public class AccountToSms(SmsService _smsService)
{
    public void Notify(AccountReport report, string phoneNumbers)
    {
        var message = new StringBuilder();
        foreach(var balance in report.Balances)
            message.AppendLine($"{balance.Name}: {balance.Balance:c0}");
        message.AppendLine($"Total: {report.Balances.Sum(b => b.Balance):c0}");
        _smsService.Send(phoneNumbers, message.ToString());
    }
}