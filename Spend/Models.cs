namespace Nabster.Spend;

public class SpendReport
{
    public string BudgetName { get; set; } = string.Empty;
    public List<SpendGroup> Groups { get; set; } = [];
    public decimal MonthlyTotal { get; set; }
    public decimal YearlyTotal { get; set; }
}

public class SpendGroup
{
    public string CategoryGroupName { get; set; } = string.Empty;
    public List<SpendCategory> Categories { get; set; } = [];
    public decimal MonthlyTotal { get; set; }
    public decimal YearlyTotal { get; set; }
}

public class SpendCategory
{
    public string CategoryName { get; set; } = string.Empty;
    public string GoalCadence { get; set; } = string.Empty;
    public string GoalDay { get; set; } = string.Empty;
    public decimal GoalTarget { get; set; }
    public decimal? GoalPercentageComplete { get; set; }
    public decimal MonthlyCost { get; set; }
}