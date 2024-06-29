using Nabster.Domain.Reports;
using Nabster.Domain.Services;

namespace Nabster.Domain.Notifications;

public class MessageToSms(SmsService _smsService)
{
    public void Notify(string message, string phoneNumbers)
    {
        _smsService.Send(phoneNumbers, message);
    }
}