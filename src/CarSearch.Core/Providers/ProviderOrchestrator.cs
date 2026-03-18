using CarSearch.Models;
using Microsoft.Extensions.Logging;

namespace CarSearch.Providers;

public class ProviderOrchestrator
{
    private readonly IEnumerable<ICarSearchProvider> _providers;
    private readonly ILogger<ProviderOrchestrator> _logger;

    public ProviderOrchestrator(
        IEnumerable<ICarSearchProvider> providers,
        ILogger<ProviderOrchestrator> logger)
    {
        _providers = providers;
        _logger = logger;
    }

    public async Task<List<ProviderSearchResult>> SearchAllAsync(SearchParameters parameters, CancellationToken ct = default)
    {
        var enabledProviders = _providers.Where(p => p.IsEnabled).ToList();

        if (enabledProviders.Count == 0)
        {
            _logger.LogWarning("No providers are enabled");
            return [];
        }

        _logger.LogInformation("Starting search across {Count} provider(s): {Providers}",
            enabledProviders.Count,
            string.Join(", ", enabledProviders.Select(p => p.DisplayName)));

        // Run providers sequentially — playwright-cli uses a shared browser pipe,
        // so concurrent sessions cause EADDRINUSE conflicts.
        var results = new List<ProviderSearchResult>();
        foreach (var provider in enabledProviders)
        {
            ct.ThrowIfCancellationRequested();
            results.Add(await SearchProviderAsync(provider, parameters, ct));
        }

        var succeeded = results.Count(r => r.Success);
        var failed = results.Count(r => !r.Success);
        _logger.LogInformation("Search complete: {Succeeded} succeeded, {Failed} failed", succeeded, failed);

        return results;
    }

    private async Task<ProviderSearchResult> SearchProviderAsync(
        ICarSearchProvider provider, SearchParameters parameters, CancellationToken ct)
    {
        try
        {
            return await provider.SearchAsync(parameters, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogInformation("Provider search was canceled while running {Provider}", provider.Name);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Provider {Provider} threw an unhandled exception", provider.Name);
            return new ProviderSearchResult
            {
                ProviderName = provider.Name,
                DisplayName = provider.DisplayName,
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}
