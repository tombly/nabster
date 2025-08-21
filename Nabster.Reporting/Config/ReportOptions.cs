namespace Nabster.Reporting.Config;

/// <summary>
/// Describes the configuration options required by the Report service.
/// </summary>
public class ReportOptions
{
    public const string Section = "Reports";

    public string YnabAccessToken { get; set; } = string.Empty;
}