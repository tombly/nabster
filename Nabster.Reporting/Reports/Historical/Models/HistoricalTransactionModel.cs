namespace Nabster.Reporting.Reports.Historical.Models;

public class HistoricalTransactionModel
{
    public DateTimeOffset Date { get; set; }
    public decimal Amount { get; set; }
    public decimal RunningBalance { get; set; }
}