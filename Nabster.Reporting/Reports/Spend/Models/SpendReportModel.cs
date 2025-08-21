namespace Nabster.Reporting.Reports.Spend.Models;

public class SpendReportModel
{
    public string BudgetName { get; set; } = string.Empty;
    public string MonthName { get; set; } = string.Empty;
    public List<SpendGroupModel> Groups { get; set; } = [];
    public decimal Total => Groups.SelectMany(g => g.Transactions).Sum(t => t.Amount);
}