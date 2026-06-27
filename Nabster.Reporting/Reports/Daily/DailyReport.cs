using System.Text;
using Nabster.Reporting.Services;
using Ynab.Api.Client.Extensions;

namespace Nabster.Reporting.Reports.Daily;

public record DailyReportResult(
    string MonthName,
    int Day,
    int PercentThroughMonth,
    int MonthBarWidth,
    string MonthBar,
    IReadOnlyList<DailyReportCategory> Categories);

public record DailyReportCategory(
    string Name,
    string AmountText,
    string AmountColor,
    int? Percent,
    int BarWidth,
    string Bar,
    string BarColor,
    string TotalText);

public class DailyReport(IEnumerable<IYnabService> _ynabServices)
{
    public async Task<DailyReportResult> Build(string? budgetName, bool isDemo, string[]? categoryNames = null)
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
            .OrderBy(c => c.Name)
            .Select(c =>
            {
                var name = c.Name!;
                var activity = c.Activity.FromMilliunits();
                var available = c.Balance.FromMilliunits();

                // Use the goal's monthly target as the spending denominator so the
                // percentage stays anchored to the "amount to assign" even when the
                // assigned amount is bumped up to cover overspending. Categories
                // without a goal fall back to the assigned amount.
                var hasTarget = c.Goal_target is > 0;
                var target = hasTarget
                    ? c.Goal_target!.Value.FromMilliunits()
                    : c.Budgeted.FromMilliunits();

                // When spending exceeds the monthly target, show how far over we are
                // as a negative amount (target minus spend). The available balance
                // would read $0 here because the assigned amount gets bumped up to
                // cover the overspend, hiding the overage. Otherwise show the balance.
                var amount = hasTarget && target + activity < 0
                    ? target + activity
                    : available;

                var absActivity = Math.Abs(activity);
                var absAmount = Math.Abs(amount);
                var showsOnlyZeroAmount = target == 0 && activity == 0;

                var amountText = amount == 0
                    ? "$0"
                    : $"{(amount < 0 ? "-" : string.Empty)}${absAmount:N0}";
                var totalText = $"${target:N0}";

                int? percent = target > 0 && activity != 0
                    ? (int)Math.Round(absActivity / target * 100)
                    : null;

                var displayAmount = percent.HasValue || showsOnlyZeroAmount
                    ? amountText
                    : $"{amountText}/{totalText}";
                var amountColor = amount > 0 ? "#22c55e" : amount < 0 ? "#ef4444" : "#64748b";
                var barColor = amount < 0
                    ? "#ef4444"
                    : percent switch { < 75 => "#22c55e", <= 100 => "#f59e0b", _ => "#ef4444" };
                var barWidth = percent.HasValue ? Math.Min(percent.Value, 100) : 0;

                var bar = percent.HasValue ? BuildBar(barWidth) : string.Empty;

                return new DailyReportCategory(
                    name,
                    displayAmount,
                    amountColor,
                    percent,
                    barWidth,
                    bar,
                    barColor,
                    totalText);
            })
            .ToList();

        var pacific = TimeZoneInfo.FindSystemTimeZoneById("America/Los_Angeles");
        var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, pacific).Date;
        var daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);
        var percentThroughMonth = (int)Math.Round((double)today.Day / daysInMonth * 100);

        var monthBarWidth = Math.Min(percentThroughMonth, 100);

        return new DailyReportResult(
            today.ToString("MMMM"),
            today.Day,
            percentThroughMonth,
            monthBarWidth,
            BuildBar(monthBarWidth),
            categories);
    }

    private const int BarLength = 20;

    private static string BuildBar(int fillPercent)
    {
        var filled = (int)Math.Round(fillPercent / 100.0 * BarLength);
        return new string('█', filled) + new string('░', BarLength - filled);
    }

    public static string BuildText(DailyReportResult result)
    {
        var sb = new StringBuilder();

        sb.AppendLine("DAILY SUMMARY");
        sb.AppendLine($"{result.MonthName} {result.Day}");
        sb.AppendLine();

        foreach (var c in result.Categories)
        {
            sb.AppendLine($"{c.Name}: {c.AmountText}");
            if (c.Percent is { } percent)
                sb.AppendLine($"  {c.Bar} {percent}% of {c.TotalText} spent");
        }

        sb.AppendLine();
        sb.AppendLine($"{result.MonthName} {result.Day} — {result.PercentThroughMonth}% through the month");
        sb.AppendLine(result.MonthBar);

        return sb.ToString().TrimEnd();
    }

    public static string BuildHtml(DailyReportResult result)
    {
        var sb = new StringBuilder();

        const string bgBody = "background-image:url('data:image/svg+xml;utf8,<svg xmlns=&quot;http://www.w3.org/2000/svg&quot; width=&quot;10&quot; height=&quot;10&quot;><rect fill=&quot;%231e293b&quot; width=&quot;10&quot; height=&quot;10&quot;/></svg>');";
        const string bgHeader = "background-image:url('data:image/svg+xml;utf8,<svg xmlns=&quot;http://www.w3.org/2000/svg&quot; width=&quot;10&quot; height=&quot;10&quot;><rect fill=&quot;%230f172a&quot; width=&quot;10&quot; height=&quot;10&quot;/></svg>');";

        sb.Append($"""
            <!DOCTYPE html>
            <html>
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width,initial-scale=1">
              <meta name="color-scheme" content="light dark">
              <meta name="supported-color-schemes" content="light dark">
            </head>
            <body class="body-bg" style="margin:0;padding:0;background-color:#1e293b;{bgBody}font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Arial,sans-serif;">
            <table width="100%" cellpadding="0" cellspacing="0" border="0" class="body-bg" style="background-color:#1e293b;{bgBody}">
            <tr><td align="center" class="body-bg" style="padding:32px 16px;background-color:#1e293b;{bgBody}">
            <table width="560" cellpadding="0" cellspacing="0" border="0" style="max-width:560px;width:100%;">

            <tr><td class="header-bg" style="background-color:#0f172a;{bgHeader}border-radius:16px 16px 0 0;padding:36px 32px;text-align:center;">
              <p style="margin:0 0 8px 0;color:#818cf8;font-size:11px;font-weight:700;letter-spacing:3px;text-transform:uppercase;">Daily Summary</p>
              <p style="margin:0;color:#ffffff;font-size:40px;font-weight:800;letter-spacing:-1px;">{result.MonthName} {result.Day}</p>
            </td></tr>

            <tr><td class="header-bg" style="background-color:#0f172a;{bgHeader}padding:16px 24px 8px 24px;">
            """);

        foreach (var c in result.Categories)
        {
            sb.Append($"""
                <table width="100%" cellpadding="0" cellspacing="0" style="margin-bottom:8px;">
                <tr><td class="card-bg" style="padding:14px 16px;background-color:#1e293b;{bgBody}border-radius:10px;">
                  <table width="100%" cellpadding="0" cellspacing="0">
                    <tr>
                      <td style="color:#e2e8f0;font-size:14px;font-weight:600;">{c.Name}</td>
                      <td align="right" style="color:{c.AmountColor};font-size:14px;font-weight:700;">{c.AmountText}</td>
                    </tr>
                """);

            if (c.Percent is { } percent)
            {
                sb.Append($"""
                    <tr><td colspan="2" style="padding-top:8px;">
                      <table width="100%" cellpadding="0" cellspacing="0"><tr>
                        <td class="bar-track" style="background-color:#334155;border-radius:4px;height:5px;overflow:hidden;">
                          <table cellpadding="0" cellspacing="0" width="{c.BarWidth}%"><tr>
                            <td style="background-color:{c.BarColor};height:5px;"></td>
                          </tr></table>
                        </td>
                      </tr></table>
                      <p style="margin:4px 0 0 0;text-align:right;font-size:11px;color:#94a3b8;">{percent}% of {c.TotalText} spent</p>
                    </td></tr>
                    """);
            }

            sb.Append("</table></td></tr></table>");
        }

        sb.Append($"""
            </td></tr>

            <tr><td class="footer-bg" style="background-color:#0f172a;{bgHeader}border-radius:0 0 16px 16px;padding:20px 32px;">
              <table width="100%" cellpadding="0" cellspacing="0">
                <tr><td style="color:#94a3b8;font-size:13px;padding-bottom:8px;">{result.MonthName} {result.Day} &mdash; {result.PercentThroughMonth}% through the month</td></tr>
                <tr><td>
                  <table width="100%" cellpadding="0" cellspacing="0"><tr>
                    <td class="month-track" style="background-color:#1e293b;border-radius:4px;height:6px;overflow:hidden;">
                      <table cellpadding="0" cellspacing="0" width="{result.MonthBarWidth}%"><tr>
                        <td style="background-color:#818cf8;height:6px;"></td>
                      </tr></table>
                    </td>
                  </tr></table>
                </td></tr>
              </table>
            </td></tr>

            </table>
            </td></tr>
            </table>
            </body>
            </html>
            """);

        return sb.ToString();
    }
}
