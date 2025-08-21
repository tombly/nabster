namespace Nabster.Chat.Config;

/// <summary>
/// Describes the configuration options required by the Chat service.
/// </summary>
public class ChatOptions
{
    public const string Section = "Chat";

    public string YnabAccessToken { get; set; } = string.Empty;
    public string OpenAiUrl { get; set; } = string.Empty;
    public string TwilioAccountSid { get; set; } = string.Empty;
    public string TwilioAuthToken { get; set; } = string.Empty;
    public string TwilioPhoneNumber { get; set; } = string.Empty;
}