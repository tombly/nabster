using Nabster.Domain.Reports;
using Nabster.Domain.Services;

namespace Nabster.Domain.Notifications;

public class MessageToSms(SmsService _smsService)
{
    public void Notify(string phoneNumbers, string message)
    {
        _smsService.Send(phoneNumbers, message);
    }
}