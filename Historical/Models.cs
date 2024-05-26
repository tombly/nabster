namespace Nabster.Historical;

public class HistoricalReport
{
    public string BudgetName { get; set; } = string.Empty;
    public List<HistoricalAccountGroup> AccountGroups { get; set; } = [];
}

public class HistoricalAccountGroup
{
    public string Name { get; set; } = string.Empty;
    public List<string> AccountNames { get; set; } = [];
    public List<HistoricalTransaction> Transactions { get; set; } = [];
}

public class HistoricalTransaction
{
    public DateTimeOffset Date { get; set; }
    public decimal Amount { get; set; }
    public decimal CumulativeAmount { get; set; }
}