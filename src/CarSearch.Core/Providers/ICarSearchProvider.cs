using CarSearch.Models;

namespace CarSearch.Providers;

public interface ICarSearchProvider
{
    string Name { get; }
    string DisplayName { get; }
    bool IsEnabled { get; }
    Task<ProviderSearchResult> SearchAsync(SearchParameters parameters, CancellationToken ct = default);
}
