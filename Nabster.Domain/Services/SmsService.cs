﻿using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Nabster.Domain.Services;

public class SmsService
{
    private readonly string _twilioPhoneNumber;

    public SmsService()
    {
        _twilioPhoneNumber = Environment.GetEnvironmentVariable("TWILIO_PHONE_NUMBER") ?? throw new Exception("TWILIO_PHONE_NUMBER not set");
        var twilioAccountSid = Environment.GetEnvironmentVariable("TWILIO_ACCOUNT_SID") ?? throw new Exception("TWILIO_ACCOUNT_SID not set");
        var twilioAuthToken = Environment.GetEnvironmentVariable("TWILIO_AUTH_TOKEN") ?? throw new Exception("TWILIO_AUTH_TOKEN not set");

        TwilioClient.Init(twilioAccountSid, twilioAuthToken);
    }

    public void Send(string phoneNumbers, string message)
    {
        foreach (var phoneNumber in phoneNumbers.Split(','))
        {
            MessageResource.Create(
                from: new PhoneNumber(_twilioPhoneNumber),
                to: new PhoneNumber(phoneNumber),
                body: message);
        }
    }
}