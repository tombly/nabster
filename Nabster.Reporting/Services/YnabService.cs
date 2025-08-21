using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using Nabster.Reporting.Config;
using Ynab.Api.Client;

namespace Nabster.Reporting.Services;

/// <summary>
/// A DI-friendly wrapper around <see cref="YnabApiClient"/>.
/// </summary>
public class YnabService
{
    public YnabApiClient Client { get; private set; }

    public YnabService(IOptions<ReportOptions> options)
    {
        if (string.IsNullOrEmpty(options?.Value?.YnabAccessToken))
            throw new InvalidOperationException("YnabAccessToken is not set in ReportOptions.");

        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.Value.YnabAccessToken);
        Client = new YnabApiClient(httpClient);
    }
}