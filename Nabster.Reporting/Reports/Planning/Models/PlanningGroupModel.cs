namespace Nabster.Reporting.Reports.Planning.Models;

public class PlanningGroupModel
{
    public string CategoryGroupName { get; set; } = string.Empty;
    public List<PlanningCategoryModel> Categories { get; set; } = [];
    public decimal MonthlyTotal { get; set; }
    public decimal YearlyTotal { get; set; }
}