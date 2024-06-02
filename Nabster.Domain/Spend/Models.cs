namespace Nabster.Spend;

public class SpendReport
{
    public string BudgetName { get; set; } = string.Empty;
    public List<SpendGroup> Groups { get; set; } = [];
    public decimal Total => Groups.SelectMany(g => g.Transactions).Sum(t => t.Amount);
}

public class SpendGroup
{
    public string MemoPrefix { get; set; } = string.Empty;
    public List<SpendTransaction> Transactions { get; set; } = [];
    public decimal Total => Transactions.Sum(t => t.Amount);
}

public class SpendTransaction
{
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset Date { get; set; }
    public decimal Amount { get; set; }
}