namespace Nabster.Reporting.Reports.Spend.Models;

public class SpendTransactionModel
{
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset Date { get; set; }
    public decimal Amount { get; set; }
}