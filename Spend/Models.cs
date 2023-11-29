namespace Nabster.Spend;

public class SpendReport
{
    public string BudgetName { get; set; } = string.Empty;
    public List<SpendCategory> Categories { get; set; } = [];
}

public class SpendCategory
{
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryGroupName { get; set; } = string.Empty;
    public string GoalCadence { get; set; } = string.Empty;
    public string GoalDay { get; set; } = string.Empty;
    public string GoalTarget { get; set; } = string.Empty;
    public string GoalPercentageComplete { get; internal set; } = string.Empty;
    public decimal? MonthlyCost { get; set; }
}