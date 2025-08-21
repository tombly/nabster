namespace Nabster.Reporting.Reports.Historical.Models;

public class HistoricalAccountModel
{
    public string Name { get; set; } = string.Empty;
    public List<HistoricalTransactionModel> Transactions { get; set; } = [];
}