using System.Text;
using Nabster.Reports.Generators;

namespace Nabster.Reports.Exporters;

public static class PlanningToHtml
{
    public static byte[] Create(PlanningReport report)
    {
        var html = new StringBuilder();
        html.AppendLine($"<html><head><meta charset=\"utf-8\"></head><body style=\"font-family: monospace;\">");

        CreateStyle(html);
        CreateHeaderRow(html);
        foreach (var group in report.Groups)
        {
            CreateGroupTitleRow(html, group.CategoryGroupName);
            foreach (var category in group.Categories)
                CreateCategoryRow(html, category);
            CreateGroupTotalRow(html, group.MonthlyTotal, group.YearlyTotal);
        }
        CreateReportTotalRow(html, report.MonthlyTotal, report.YearlyTotal);

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
                    flex: 0 0 100px;
                    border-bottom: 1px solid #dddddd;
                }

                .flex-column-wide {
                    flex: 0 0 350px;
                    border-bottom: 1px solid #dddddd;
                }

                .no-underline {
                    border: none;
                }

                .align-right {
                    text-align: right;
                }
            </style>");
    }

    private static void CreateHeaderRow(StringBuilder html)
    {
        html.AppendLine("<div class='flex-row'>");
        html.AppendLine("<div class='flex-column-wide'>Category</div>");
        html.AppendLine("<div class='flex-column'>Cadence</div>");
        html.AppendLine("<div class='flex-column'>Day</div>");
        html.AppendLine("<div class='flex-column'>Yearly</div>");
        html.AppendLine("<div class='flex-column'>Monthly</div>");
        html.AppendLine("<div class='flex-column'>Progress</div>");
        html.AppendLine("</div>");
    }

    private static void CreateGroupTitleRow(StringBuilder html, string categoryGroupName)
    {
        html.AppendLine($"<b>{categoryGroupName}</b>");
    }

    private static void CreateCategoryRow(StringBuilder html, PlanningCategory category)
    {
        html.AppendLine("<div class='flex-row'>");
        html.AppendLine($"<div class='flex-column-wide'>{category.CategoryName}</div>");
        html.AppendLine($"<div class='flex-column'>{category.GoalCadence}</div>");
        html.AppendLine($"<div class='flex-column'>{category.GoalDay}</div>");
        html.AppendLine($"<div class='flex-column align-right'>{category.YearlyCost:C}</div>");
        html.AppendLine($"<div class='flex-column align-right'>{category.MonthlyCost:C}</div>");

        if (category.GoalPercentageComplete != null)
            html.AppendLine($"<div class='flex-column align-right'>{category.GoalPercentageComplete:P0}</div>");

        html.AppendLine("</div>");
    }

    private static void CreateGroupTotalRow(StringBuilder html, decimal monthlyTotal, decimal yearlyTotal)
    {
        html.AppendLine("<div class='flex-row'>");
        html.AppendLine($"<div class='flex-column-wide no-underline'></div>");
        html.AppendLine($"<div class='flex-column no-underline'></div>");
        html.AppendLine($"<div class='flex-column no-underline'><b>Group Total</b></div>");
        html.AppendLine($"<div class='flex-column no-underline align-right'><b>{yearlyTotal:C}</b></div>");
        html.AppendLine($"<div class='flex-column no-underline align-right'><b>{monthlyTotal:C}</b></div>");
        html.AppendLine("</div>");
    }

    private static void CreateReportTotalRow(StringBuilder html, decimal monthlyTotal, decimal yearlyTotal)
    {
        html.AppendLine("<div>&nbsp;</div>");
        html.AppendLine("<div class='flex-row'>");
        html.AppendLine($"<div class='flex-column-wide no-underline'></div>");
        html.AppendLine($"<div class='flex-column no-underline'></div>");
        html.AppendLine($"<div class='flex-column no-underline'><b>Total</b></div>");
        html.AppendLine($"<div class='flex-column no-underline align-right'><b>{yearlyTotal:C}</b></div>");
        html.AppendLine($"<div class='flex-column no-underline align-right'><b>{monthlyTotal:C}</b></div>");
        html.AppendLine("</div>");
    }
}