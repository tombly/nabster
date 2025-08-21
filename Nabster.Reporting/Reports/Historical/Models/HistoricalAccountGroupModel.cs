namespace Nabster.Reporting.Reports.Historical.Models;

public class HistoricalAccountGroupModel
{
    public string Name { get; set; } = string.Empty;
    public string Prefix { get; set; } = string.Empty;
    public List<HistoricalAccountModel> Accounts { get; set; } = [];
    public List<HistoricalTransactionModel> AllTransactions { get; set; } = [];
}