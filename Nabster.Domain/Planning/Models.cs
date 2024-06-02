namespace Nabster.Planning;

public class PlanningReport
{
    public string BudgetName { get; set; } = string.Empty;
    public List<PlanningGroup> Groups { get; set; } = [];
    public decimal MonthlyTotal { get; set; }
    public decimal YearlyTotal { get; set; }
}

public class PlanningGroup
{
    public string CategoryGroupName { get; set; } = string.Empty;
    public List<PlanningCategory> Categories { get; set; } = [];
    public decimal MonthlyTotal { get; set; }
    public decimal YearlyTotal { get; set; }
}

public class PlanningCategory
{
    public string CategoryName { get; set; } = string.Empty;
    public string GoalCadence { get; set; } = string.Empty;
    public string GoalDay { get; set; } = string.Empty;
    public decimal GoalTarget { get; set; }
    public decimal? GoalPercentageComplete { get; set; }
    public decimal MonthlyCost { get; set; }
}