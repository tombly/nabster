using Nabster.Domain.Reports;
using Nabster.Domain.Services;

namespace Nabster.Domain.Notifications;

public class CategoryActivityToSms(SmsService smsService)
{
    private readonly SmsService _smsService = smsService;

    public void Notify(string categoryName, string phoneNumbers, CategoryActivityReport report)
    {
        var message = default(string);
        if (report.Target != null)
            message = $"Current {categoryName} spending: {report.Activity:c0} of {report.Target:c0} ({report.Activity / report.Target:p0})";
        else
            message = $"Current {categoryName} spending: {report.Activity:c0}";
        _smsService.Send(phoneNumbers, message);
    }
}