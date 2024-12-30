using System.CommandLine.Binding;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Nabster.Chat;
using Nabster.Cli.Options;

namespace Nabster.Cli.Binders;

public class SmsClientBinder : BinderBase<SmsClient>
{
    private SmsClient? _smsClient;

    protected override SmsClient GetBoundValue(BindingContext bindingContext)
    {
        if (_smsClient is null)
        {
            var configFileName = bindingContext.ParseResult.GetValueForOption(ConfigFileOption.Value);
            var builder = new ConfigurationBuilder()
                            .SetBasePath(Directory.GetCurrentDirectory())
                            .AddJsonFile(configFileName!, optional: false);
            var config = builder.Build();

            var keyVaultUrl = config["KeyVaultUrl"] ?? throw new InvalidOperationException("KeyVaultUrl not found in config file.");

            var client = new SecretClient(vaultUri: new Uri(keyVaultUrl), credential: new DefaultAzureCredential());
            Environment.SetEnvironmentVariable("TWILIO_ACCOUNT_SID", client.GetSecret("TwilioAccountSid").Value.Value);
            Environment.SetEnvironmentVariable("TWILIO_AUTH_TOKEN", client.GetSecret("TwilioAuthToken").Value.Value);
            Environment.SetEnvironmentVariable("TWILIO_PHONE_NUMBER", client.GetSecret("TwilioPhoneNumber").Value.Value);

            _smsClient = new();
        }

        return _smsClient;
    }
}