using System.Diagnostics;
using CarSearch.Configuration;
using CarSearch.Models;
using CarSearch.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CarSearch.Providers.Platforms.D2cMedia;

public abstract class D2cMediaProviderBase<TParser> : ICarSearchProvider
    where TParser : ID2cMediaSnapshotParser
{
    private readonly TParser _parser;
    private readonly PlaywrightCliService _playwrightCli;
    private readonly ProviderOptions _options;
    private readonly D2cMediaProviderDefinition _definition;
    private readonly ILogger _logger;

    protected D2cMediaProviderBase(
        TParser parser,
        PlaywrightCliService playwrightCli,
        IOptionsSnapshot<ProviderOptions> options,
        ILogger logger,
        D2cMediaProviderDefinition definition)
    {
        _parser = parser;
        _playwrightCli = playwrightCli;
        _definition = definition;
        _logger = logger;
        _options = options.Get(definition.Name);
    }

    public string Name => _definition.Name;
    public string DisplayName => _definition.DisplayName;
    public bool IsEnabled => _options.Enabled;

    public async Task<ProviderSearchResult> SearchAsync(SearchParameters parameters, CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new ProviderSearchResult
        {
            ProviderName = Name,
            DisplayName = DisplayName
        };

        var cli = _playwrightCli.CreateSession(parameters.TimeoutMs, ct);

        try
        {
            var inventoryUrl = _options.BaseUrl.TrimEnd('/') + _definition.InventoryPath;
            _logger.LogInformation("[{Provider}] Opening {Url}...", Name, inventoryUrl);
            await cli.OpenAsync(inventoryUrl);
            await cli.WaitAsync(3000);

            _logger.LogInformation("[{Provider}] Selecting make: {Make}", Name, parameters.Make);
            var yaml = await cli.SnapshotWithRetryAsync();
            var makeRef = _parser.FindListItemRef(yaml, parameters.Make);
            if (makeRef != null)
            {
                await cli.ClickAsync(makeRef);
                await cli.WaitAsync(2000);
            }

            _logger.LogInformation("[{Provider}] Selecting model: {Model}", Name, parameters.Model);
            yaml = await cli.SnapshotWithRetryAsync();
            var modelRef = _parser.FindListItemRef(yaml, parameters.Model);
            if (modelRef != null)
            {
                await cli.ClickAsync(modelRef);
                await cli.WaitAsync(2000);
            }

            if (!string.IsNullOrWhiteSpace(parameters.Color))
            {
                _logger.LogInformation("[{Provider}] Applying color filter: {Color}", Name, parameters.Color);
                yaml = await cli.SnapshotWithRetryAsync();
                var colorRef = _parser.FindColorRef(yaml, parameters.Color);
                if (colorRef != null)
                {
                    await cli.ClickAsync(colorRef);
                    await cli.WaitAsync(2000);
                }
                else
                {
                    _logger.LogWarning("[{Provider}] Could not find color filter for: {Color}", Name, parameters.Color);
                }
            }

            _logger.LogInformation("[{Provider}] Parsing search results...", Name);
            await cli.WaitAsync(2000);
            yaml = await cli.SnapshotWithRetryAsync();

            result.TotalCount = _parser.ParseResultCount(yaml);
            result.City = _parser.ParseCity(yaml);
            result.Listings = _parser.ParseListings(yaml, CreateListingContext());
            result.Success = true;

            _logger.LogInformation(
                "[{Provider}] Found {Count} listings (total results: {Total})",
                Name,
                result.Listings.Count,
                result.TotalCount);

            await cli.CloseAsync();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            await cli.CloseAsync();
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{Provider}] Search failed", Name);
            result.Success = false;
            result.ErrorMessage = ex.Message;
            await cli.CloseAsync();
        }

        stopwatch.Stop();
        result.Duration = stopwatch.Elapsed;
        return result;
    }

    protected virtual D2cMediaListingContext CreateListingContext()
    {
        return new D2cMediaListingContext(
            _definition.DealerName ?? DisplayName,
            Name,
            _options.BaseUrl);
    }
}
