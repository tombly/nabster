using Microsoft.Extensions.DependencyInjection;
using Nabster.Chat.Services;

namespace Nabster.Chat.Config;

/// <summary>
/// Registers necessary services for the Chat feature.
/// </summary>
public static class DependencyModule
{
    public static IServiceCollection AddNabsterChat(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddTransient<ChatService>();
        services.AddTransient<SmsService>();
        services.AddTransient<EmailService>();
        services.AddTransient<ChatCompletionService>();
        services.AddTransient<YnabService>();

        return services;
    }
}