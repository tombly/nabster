using System.Text;
using Nabster.Domain.Reports;
using Nabster.Domain.Services;

namespace Nabster.Domain.Notifications;

/// <summary>
/// Generates a simple report that shows how much of a single category has been
/// funded (including the amounts) or a category group (including just the
/// percentages for each).
/// </summary>
public class FundedToSms(SmsService _smsService)
{
    public void Notify(string phoneNumbers, FundedReport report)
    {
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

        foreach(var category in report.Categories)
        {
            var funded = category.Funded;
            var target = category.Target;
            var percentage = funded / target;

            message.AppendLine($"{category.Name}: ({percentage:p0})");            
        }

        return message.ToString();
    }
}