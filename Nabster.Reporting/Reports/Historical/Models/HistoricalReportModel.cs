namespace Nabster.Reporting.Reports.Historical.Models;

public class HistoricalReportModel
{
    public string BudgetName { get; set; } = string.Empty;
    public List<HistoricalAccountGroupModel> AccountGroups { get; set; } = [];
}