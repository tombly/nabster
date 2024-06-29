using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Nabster.Domain.Notifications;
using Nabster.Domain.Services;
using Ynab.Api.Client;

namespace Nabster.Domain.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNabsterDomain(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddTransient<Reports.Activity>();
        services.AddTransient<Reports.Funded>();
        services.AddTransient<Reports.Performance>();
        services.AddTransient<Reports.Planning>();
        services.AddTransient<Reports.Spend>();
        services.AddTransient<ActivityToSms>();
        services.AddTransient<FundedToSms>();
        services.AddTransient<MessageToSms>();
        services.AddTransient<CalculateService>();
        services.AddTransient<MessagingService>();
        services.AddSingleton<YnabApiClient>(sp =>
        {
            var ynabAccessToken = Environment.GetEnvironmentVariable("YNAB_ACCESS_TOKEN") ?? throw new Exception("YNAB_ACCESS_TOKEN not set");
            return new YnabApiClient(new HttpClient()
            {
                DefaultRequestHeaders = {
                    Authorization = new AuthenticationHeaderValue("Bearer", ynabAccessToken)
                }
            });
        });
        services.AddSingleton<SmsService>();

        return services;
    }
}