using Microsoft.Extensions.Options;
using Nabster.Chat.Config;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Nabster.Chat.Services;

/// <summary>
/// A DI-friendly wrapper around <see cref="TwilioClient"/>.
/// </summary>
public class SmsService
{
    private readonly string _twilioPhoneNumber;

    public SmsService(IOptions<ChatOptions> chatOptions)
    {
        _twilioPhoneNumber = chatOptions.Value.TwilioPhoneNumber ?? throw new Exception("TwilioPhoneNumber not set");
        var twilioAccountSid = chatOptions.Value.TwilioAccountSid ?? throw new Exception("TwilioAccountSid not set");
        var twilioAuthToken = chatOptions.Value.TwilioAuthToken ?? throw new Exception("TwilioAuthToken not set");

        TwilioClient.Init(twilioAccountSid, twilioAuthToken);
    }

    public void Send(string phoneNumbers, string message)
    {
        if (message.Length > 160)
            message = string.Concat(message.AsSpan(0, 157), "...");

        foreach (var phoneNumber in phoneNumbers.Split(','))
        {
            MessageResource.Create(
                from: new PhoneNumber(_twilioPhoneNumber),
                to: new PhoneNumber(phoneNumber),
                body: message);
        }
    }
}
