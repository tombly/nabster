namespace Nabster.Reporting.Reports.Planning.Models;

public class PlanningReportModel
{
    public string BudgetName { get; set; } = string.Empty;
    public List<PlanningGroupModel> Groups { get; set; } = [];
    public decimal MonthlyTotal { get; set; }
    public decimal YearlyTotal { get; set; }
}