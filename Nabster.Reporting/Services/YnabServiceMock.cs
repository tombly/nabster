using Ynab.Api.Client;
using Ynab.Api.Client.Mock;

namespace Nabster.Reporting.Services;

/// <summary>
/// A DI-friendly wrapper around <see cref="YnabApiClientMock"/>.
/// </summary>
public class YnabServiceMock() : IYnabService
{
    public IYnabApiClient Client { get; private set; } = new YnabApiClientMock();
    public bool IsDemo { get; private set; } = true;
}