using System.Diagnostics;
using CarSearch.Configuration;
using CarSearch.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CarSearch.Providers.Clutch;

public class ClutchProvider : ICarSearchProvider
{
    private readonly ProviderOptions _options;
    private readonly ILogger<ClutchProvider> _logger;

    public string Name => "Clutch";
    public string DisplayName => "Clutch.ca";
    public bool IsEnabled => _options.Enabled;

    public ClutchProvider(
        IOptionsSnapshot<ProviderOptions> options,
        ILogger<ClutchProvider> logger)
    {
        _options = options.Get("Clutch");
        _logger = logger;
    }

    public Task<ProviderSearchResult> SearchAsync(SearchParameters parameters, CancellationToken ct = default)
    {
        _logger.LogWarning("[{Provider}] Provider not yet implemented", Name);

        return Task.FromResult(new ProviderSearchResult
        {
            ProviderName = Name,
            DisplayName = DisplayName,
            Success = false,
            ErrorMessage = "Provider not yet implemented"
        });
    }
}
