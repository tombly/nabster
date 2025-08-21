using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using Nabster.Chat.Config;
using Ynab.Api.Client;

namespace Nabster.Chat.Services;

/// <summary>
/// A DI-friendly wrapper around <see cref="YnabApiClient"/>.
/// </summary>
public class YnabService
{
    public YnabApiClient Client { get; private set; }

    public YnabService(IOptions<ChatOptions> options)
    {
        if (options.Value.YnabAccessToken == null)
        {
            throw new InvalidOperationException("YnabAccessToken is not set in ChatOptions.");
        }

        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.Value.YnabAccessToken);
        Client = new YnabApiClient(httpClient);
    }
}