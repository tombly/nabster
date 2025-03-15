using Microsoft.Extensions.DependencyInjection;

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