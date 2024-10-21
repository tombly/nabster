using System.Text;
using Nabster.Domain.Reports;

namespace Nabster.Domain.Exports;

public static class SpendToHtml
{
    public static byte[] Create(SpendReport report)
    {
        var html = new StringBuilder();
        html.AppendLine($"<html><body style=\"font-family: monospace;\">");

        CreateStyle(html);
        CreateTitleRow(html, report);
        foreach (var group in report.Groups.OrderBy(g => g.MemoPrefix))
        {
            CreateGroupTitleRow(html, group.MemoPrefix);
            foreach (var transaction in group.Transactions)
                CreateTransactionRow(html, transaction);
            CreateGroupTotalRow(html, group.Total);
        }
        CreateReportTotalRow(html, report.Total);

        html.AppendLine($"</body></html>");

        return Encoding.UTF8.GetBytes(html.ToString());
    }

    private static void CreateStyle(StringBuilder html)
    {
        html.AppendLine(@"
            <style>
                .flex-row {
                    display: flex;
                }

                .flex-column {
                    flex: 0 0 110px;
                    border-bottom: 1px solid #dddddd;
                }

                .flex-column-wide {
                    flex: 0 0 450px;
                    border-bottom: 1px solid #dddddd;
                }

                .no-underline {
                    border: none;
                }
            </style>");
    }

    private static void CreateTitleRow(StringBuilder html, SpendReport report)
    {
        html.AppendLine($"<div><b>Spend Report - {report.BudgetName} - {report.MonthName}</b></div>");
        html.AppendLine("<div>&nbsp;</div>");
    }

    private static void CreateGroupTitleRow(StringBuilder html, string categoryGroupName)
    {
        html.AppendLine($"<div><b>{categoryGroupName}</b></div>");
    }

    private static void CreateTransactionRow(StringBuilder html, SpendTransaction transaction)
    {
        html.AppendLine("<div class='flex-row'>");
        html.AppendLine($"<div class='flex-column-wide'>{transaction.Description}</div>");
        html.AppendLine($"<div class='flex-column'>{transaction.Date.DateTime.ToShortDateString()}</div>");
        html.AppendLine($"<div class='flex-column'>{transaction.Amount:C}</div>");
        html.AppendLine("</div>");
    }

    private static void CreateGroupTotalRow(StringBuilder html, decimal total)
    {
        html.AppendLine("<div class='flex-row'>");
        html.AppendLine($"<div class='flex-column-wide no-underline'></div>");
        html.AppendLine($"<div class='flex-column no-underline'><b>Total</b></div>");
        html.AppendLine($"<div class='flex-column no-underline'><b>{total:C}</b></div>");
        html.AppendLine("</div>");
    }

    private static void CreateReportTotalRow(StringBuilder html, decimal total)
    {
        html.AppendLine("<div>&nbsp;</div>");
        html.AppendLine("<div class='flex-row'>");
        html.AppendLine($"<div class='flex-column-wide no-underline'></div>");
        html.AppendLine($"<div class='flex-column no-underline'><b>Report Total</b></div>");
        html.AppendLine($"<div class='flex-column no-underline'><b>{total:C}</b></div>");
        html.AppendLine("</div>");
    }
}