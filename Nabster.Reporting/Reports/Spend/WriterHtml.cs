using System.Text;
using Nabster.Reporting.Reports.Spend.Models;

namespace Nabster.Reporting.Reports.Spend;

public static class SpendToHtml
{
    // Tracks the row index for alternating background colors.
    private static int _rowIndex = 0;

    public static byte[] ToHtml(this SpendReportModel report)
    {
        _rowIndex = 0;

        var html = new StringBuilder();
        html.AppendLine($"<html><head><meta charset=\"utf-8\"></head><body style=\"font-family: monospace;\">");

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
                    border-bottom: 1px solid rgba(255, 255, 255, 0);
                }

                .flex-column-wide {
                    flex: 0 0 450px;
                    border-bottom: 1px solid rgba(255, 255, 255, 0);
                }

                .no-underline {
                    border: none;
                }
            </style>");
    }

    private static void CreateTitleRow(StringBuilder html, SpendReportModel report)
    {
        html.AppendLine($"<div><b>Spend Report - {report.BudgetName} - {report.MonthName}</b></div>");
        html.AppendLine("<div>&nbsp;</div>");
    }

    private static void CreateGroupTitleRow(StringBuilder html, string categoryGroupName)
    {
        html.AppendLine($"<div><b>{categoryGroupName}</b></div>");
    }

    private static void CreateTransactionRow(StringBuilder html, SpendTransactionModel transaction)
    {
        var bgColor = (_rowIndex++ % 2 == 0) ? "#f6f6f6" : "#ffffff";
        html.AppendLine($"<div class='flex-row' style='background-color: {bgColor};'>");
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