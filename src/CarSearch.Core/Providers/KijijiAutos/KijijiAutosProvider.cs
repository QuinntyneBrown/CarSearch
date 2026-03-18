using System.Diagnostics;
using CarSearch.Configuration;
using CarSearch.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CarSearch.Providers.KijijiAutos;

public class KijijiAutosProvider : ICarSearchProvider
{
    private readonly ProviderOptions _options;
    private readonly ILogger<KijijiAutosProvider> _logger;

    public string Name => "KijijiAutos";
    public string DisplayName => "Kijiji Autos";
    public bool IsEnabled => _options.Enabled;

    public KijijiAutosProvider(
        IOptionsSnapshot<ProviderOptions> options,
        ILogger<KijijiAutosProvider> logger)
    {
        _options = options.Get("KijijiAutos");
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
