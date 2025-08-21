namespace Nabster.Reporting.Reports.Planning.Models;

public class PlanningCategoryModel
{
    public string CategoryName { get; set; } = string.Empty;
    public string GoalCadence { get; set; } = string.Empty;
    public string GoalDay { get; set; } = string.Empty;
    public decimal GoalTarget { get; set; }
    public decimal? GoalPercentageComplete { get; set; }
    public decimal MonthlyCost { get; set; }
    public decimal YearlyCost { get; set; }
}