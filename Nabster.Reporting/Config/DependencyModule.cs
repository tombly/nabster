using Microsoft.Extensions.DependencyInjection;
using Nabster.Reporting.Reports.Historical;
using Nabster.Reporting.Reports.Planning;
using Nabster.Reporting.Reports.Spend;
using Nabster.Reporting.Services;

namespace Nabster.Reporting.Config;

/// <summary>
/// Registers necessary services for the Reporting feature.
/// </summary>
public static class DependencyModule
{
    public static IServiceCollection AddNabsterReports(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddTransient<IYnabService, YnabServiceMock>();
        services.AddTransient<IYnabService, YnabService>();
        services.AddTransient<HistoricalReport>();
        services.AddTransient<PlanningReport>();
        services.AddTransient<SpendReport>();
        services.AddTransient<MemoService>();

        return services;
    }
}