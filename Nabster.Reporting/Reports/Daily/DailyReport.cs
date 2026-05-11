using System.Text;
using Nabster.Reporting.Services;
using Ynab.Api.Client.Extensions;

namespace Nabster.Reporting.Reports.Daily;

public record DailyReportResult(string Text, string Html);

public class DailyReport(IEnumerable<IYnabService> _ynabServices)
{
    private record CategoryRow(string Name, decimal Activity, decimal Budgeted);

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
            .Select(c => new CategoryRow(c.Name!, c.Activity.FromMilliunits(), c.Budgeted.FromMilliunits()))
            .ToList();

        var today = DateTime.Today;
        var daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);
        var percentThroughMonth = (int)Math.Round((double)today.Day / daysInMonth * 100);

        return new DailyReportResult(
            BuildText(categories, today, percentThroughMonth),
            BuildHtml(categories, today, percentThroughMonth));
    }

    private static string BuildText(IEnumerable<CategoryRow> categories, DateTime today, int percentThroughMonth)
    {
        var sb = new StringBuilder();

        foreach (var c in categories)
        {
            if (c.Activity == 0)
            {
                sb.AppendLine($"{c.Name}: $0");
            }
            else
            {
                var sign = c.Activity < 0 ? "-" : "+";
                var suffix = c.Budgeted > 0
                    ? $" ({(int)Math.Round(Math.Abs(c.Activity) / c.Budgeted * 100)}%)"
                    : string.Empty;
                sb.AppendLine($"{c.Name}: {sign}${Math.Abs(c.Activity):N0}{suffix}");
            }
        }

        sb.AppendLine();
        sb.AppendLine($"{today:MMMM} {today.Day} ({percentThroughMonth}%)");

        return sb.ToString().TrimEnd();
    }

    private static string BuildHtml(IEnumerable<CategoryRow> categories, DateTime today, int percentThroughMonth)
    {
        var sb = new StringBuilder();

        sb.Append($"""
            <!DOCTYPE html>
            <html>
            <head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head>
            <body style="margin:0;padding:0;background-color:#f0f2f8;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Arial,sans-serif;">
            <table width="100%" cellpadding="0" cellspacing="0" border="0" style="background-color:#f0f2f8;">
            <tr><td align="center" style="padding:32px 16px;">
            <table width="560" cellpadding="0" cellspacing="0" border="0" style="max-width:560px;width:100%;">

            <tr><td style="background-color:#0f172a;border-radius:16px 16px 0 0;padding:36px 32px;text-align:center;">
              <p style="margin:0 0 8px 0;color:#818cf8;font-size:11px;font-weight:700;letter-spacing:3px;text-transform:uppercase;">Daily Report</p>
              <p style="margin:0;color:#ffffff;font-size:40px;font-weight:800;letter-spacing:-1px;">{today:MMMM} {today.Day}</p>
            </td></tr>

            <tr><td style="background-color:#0f172a;padding:16px 24px 8px 24px;">
            """);

        foreach (var c in categories)
        {
            var absActivity = Math.Abs(c.Activity);
            var amountText = c.Activity == 0
                ? "$0"
                : $"{(c.Activity < 0 ? "-" : "+")}${absActivity:N0}";
            var amountColor = c.Activity > 0 ? "#22c55e" : c.Activity < 0 ? "#ef4444" : "#64748b";

            int? pct = c.Budgeted > 0 && c.Activity != 0
                ? (int)Math.Round(absActivity / c.Budgeted * 100)
                : null;
            var barColor = pct switch { < 50 => "#22c55e", < 80 => "#f59e0b", _ => "#ef4444" };
            var barWidth = pct.HasValue ? Math.Min(pct.Value, 100) : 0;

            sb.Append($"""
                <table width="100%" cellpadding="0" cellspacing="0" style="margin-bottom:8px;">
                <tr><td style="padding:14px 16px;background-color:#1e293b;border-radius:10px;">
                  <table width="100%" cellpadding="0" cellspacing="0">
                    <tr>
                      <td style="color:#e2e8f0;font-size:14px;font-weight:600;">{c.Name}</td>
                      <td align="right" style="color:{amountColor};font-size:14px;font-weight:700;">{amountText}</td>
                    </tr>
                """);

            if (pct.HasValue)
            {
                sb.Append($"""
                    <tr><td colspan="2" style="padding-top:8px;">
                      <table width="100%" cellpadding="0" cellspacing="0"><tr>
                        <td style="background-color:#334155;border-radius:4px;height:5px;overflow:hidden;">
                          <table cellpadding="0" cellspacing="0" width="{barWidth}%"><tr>
                            <td style="background-color:{barColor};height:5px;"></td>
                          </tr></table>
                        </td>
                      </tr></table>
                      <p style="margin:4px 0 0 0;text-align:right;font-size:11px;color:#94a3b8;">{pct}% of budget</p>
                    </td></tr>
                    """);
            }

            sb.Append("</table></td></tr></table>");
        }

        var monthBarWidth = Math.Min(percentThroughMonth, 100);

        sb.Append($"""
            </td></tr>

            <tr><td style="background-color:#0f172a;border-radius:0 0 16px 16px;padding:20px 32px;">
              <table width="100%" cellpadding="0" cellspacing="0">
                <tr><td style="color:#94a3b8;font-size:13px;padding-bottom:8px;">{today:MMMM} {today.Day} &mdash; {percentThroughMonth}% through the month</td></tr>
                <tr><td>
                  <table width="100%" cellpadding="0" cellspacing="0"><tr>
                    <td style="background-color:#1e293b;border-radius:4px;height:6px;overflow:hidden;">
                      <table cellpadding="0" cellspacing="0" width="{monthBarWidth}%"><tr>
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
