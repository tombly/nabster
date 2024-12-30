using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Ynab.Api.Client;

namespace Nabster.Chat;

public static class DependencyModule
{
    public static IServiceCollection AddNabsterChat(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddTransient<ChatService>();
        services.AddTransient<SmsClient>();

        return services;
    }
}