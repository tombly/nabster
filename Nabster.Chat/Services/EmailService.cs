using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Nabster.Chat.Config;

namespace Nabster.Chat.Services;

/// <summary>
/// Provides email sending functionality using SMTP2GO API.
/// </summary>
public class EmailService(IOptions<ChatOptions> chatOptions)
{
    private readonly string _smtp2GoApiKey = chatOptions.Value.Smtp2GoApiKey ?? throw new Exception("Smtp2GoApiKey not set");
    private readonly string _smtp2GoEmailAddress = chatOptions.Value.Smtp2GoEmailAddress ?? throw new Exception("Smtp2GoEmailAddress not set");
    private static readonly HttpClient _httpClient = new();

    /// <summary>
    /// Sends an email to one or more recipients.
    /// </summary>
    /// <param name="emailAddresses">Comma-separated list of email addresses</param>
    /// <param name="message">The message body to send</param>
    public async Task Send(string emailAddresses, string message)
    {
        foreach (var emailAddress in emailAddresses.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var body = JsonSerializer.Serialize(new
            {
                sender = _smtp2GoEmailAddress,
                to = new[] { emailAddress },
                subject = "Nabster Notification",
                text_body = message
            });

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.smtp2go.com/v3/email/send")
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("X-Smtp2go-Api-Key", _smtp2GoApiKey);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }
    }
}