using Nabster.Domain.Reports;
using Nabster.Domain.Services;

namespace Nabster.Domain.Notifications;

public class ActivityToSms(SmsService _smsService)
{
    public void Notify(ActivityReport report, string phoneNumbers)
    {
        var message = $"Current {report.Name} spending: {report.Activity:c0} of {report.Need:c0} ({report.Activity / report.Need:p0})";
        _smsService.Send(phoneNumbers, message);
    }
}