namespace Nabster.Reporting.Reports.Spend.Models;

public class SpendGroupModel
{
    public string MemoPrefix { get; set; } = string.Empty;
    public List<SpendTransactionModel> Transactions { get; set; } = [];
    public decimal Total => Transactions.Sum(t => t.Amount);
}