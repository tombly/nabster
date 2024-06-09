using Nabster.Domain.Services;

namespace Nabster.Domain.Notifications;

public class CategoryActivityToSms(SmsService smsService)
{
    private readonly SmsService _smsService = smsService;

    public void Notify(string categoryName, string phoneNumber, decimal activity)
    {
        _smsService.Send(phoneNumber, $"Current {categoryName} Spending: {Math.Abs(activity):c}");
    }
}