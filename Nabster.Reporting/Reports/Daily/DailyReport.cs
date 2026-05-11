using System.Text;
using Nabster.Reporting.Services;
using Ynab.Api.Client.Extensions;

namespace Nabster.Reporting.Reports.Daily;

/// <summary>
/// Generates a daily summary of budget category activity for the current month,
/// showing spending amounts and percentage of budget used per category.
/// </summary>
public class DailyReport(IEnumerable<IYnabService> _ynabServices)
{
    public async Task<string> Build(string? budgetName, bool isDemo, string[]? categoryNames = null)
    {
        var ynabService = _ynabServices.Single(s => s.IsDemo == isDemo)!;

        var budgetDetail = await ynabService.Client.GetBudgetDetailAsync(budgetName);

        var categoryFilter = categoryNames is { Length: > 0 }
            ? new HashSet<string>(categoryNames, StringComparer.OrdinalIgnoreCase)
            : null;

        var categories = budgetDetail.Categories!
            .Where(c => !c.Deleted)
            .Where(c => !c.Hidden)
            .Where(c => categoryFilter == null || categoryFilter.Contains(c.Name!))
            .OrderBy(c => c.Name);

        var sb = new StringBuilder();

        foreach (var category in categories)
        {
            var activity = category.Activity.FromMilliunits();
            var budgeted = category.Budgeted.FromMilliunits();

            if (activity == 0)
            {
                sb.AppendLine($"{category.Name}: $0");
            }
            else
            {
                var sign = activity < 0 ? "-" : "+";
                var suffix = budgeted > 0
                    ? $" ({(int)Math.Round(Math.Abs(activity) / budgeted * 100)}%)"
                    : string.Empty;
                sb.AppendLine($"{category.Name}: {sign}${Math.Abs(activity):N0}{suffix}");
            }
        }

        var today = DateTime.Today;
        var daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);
        var percentThroughMonth = (int)Math.Round((double)today.Day / daysInMonth * 100);
        sb.AppendLine();
        sb.AppendLine($"{today:MMMM} {today.Day} ({percentThroughMonth}%)");

        return sb.ToString().TrimEnd();
    }
}
