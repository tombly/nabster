using Ynab.Api.Client;

namespace Nabster.Reporting.Services;

public interface IYnabService
{
    IYnabApiClient Client { get; }
    bool IsDemo { get; }
}