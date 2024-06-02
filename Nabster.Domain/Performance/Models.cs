namespace Nabster.Performance;

public class PerformanceReport
{
    public string BudgetName { get; set; } = string.Empty;
    public List<PerformanceAccountGroup> AccountGroups { get; set; } = [];
}

public class PerformanceAccountGroup
{
    public string Name { get; set; } = string.Empty;
    public List<string> AccountNames { get; set; } = [];
    public List<PerformanceTransaction> Transactions { get; set; } = [];
}

public class PerformanceTransaction
{
    public DateTimeOffset Date { get; set; }
    public decimal Amount { get; set; }
    public decimal CumulativeAmount { get; set; }
}