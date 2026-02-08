using System.Diagnostics;
using CarSearch.Configuration;
using CarSearch.Models;
using CarSearch.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CarSearch.Providers.LandRoverBrampton;

public class LandRoverBramptonProvider : ICarSearchProvider
{
    private readonly LandRoverBramptonSnapshotParser _parser;
    private readonly IServiceProvider _serviceProvider;
    private readonly ProviderOptions _options;
    private readonly PlaywrightCliOptions _cliOptions;
    private readonly ILogger<LandRoverBramptonProvider> _logger;

    public string Name => "LandRoverBrampton";
    public string DisplayName => "Land Rover Brampton";
    public bool IsEnabled => _options.Enabled;

    public LandRoverBramptonProvider(
        LandRoverBramptonSnapshotParser parser,
        IServiceProvider serviceProvider,
        IOptionsSnapshot<ProviderOptions> options,
        IOptions<PlaywrightCliOptions> cliOptions,
        ILogger<LandRoverBramptonProvider> logger)
    {
        _parser = parser;
        _serviceProvider = serviceProvider;
        _options = options.Get("LandRoverBrampton");
        _cliOptions = cliOptions.Value;
        _logger = logger;
    }

    public async Task<ProviderSearchResult> SearchAsync(SearchParameters parameters, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var result = new ProviderSearchResult
        {
            ProviderName = Name,
            DisplayName = DisplayName
        };

        var cli = new PlaywrightCliService(
            Options.Create(_cliOptions),
            Microsoft.Extensions.Logging.LoggerFactoryExtensions.CreateLogger<PlaywrightCliService>(
                (ILoggerFactory)_serviceProvider.GetService(typeof(ILoggerFactory))!));

        try
        {
            // Step 1: Open the pre-owned inventory page
            var inventoryUrl = _options.BaseUrl.TrimEnd('/') + "/used/search.html";
            _logger.LogInformation("[{Provider}] Opening {Url}...", Name, inventoryUrl);
            await cli.OpenAsync(inventoryUrl);
            await cli.WaitAsync(3000);

            // Step 2: Click on the model filter to select the desired model
            _logger.LogInformation("[{Provider}] Selecting make: {Make}", Name, parameters.Make);
            var yaml = await cli.SnapshotWithRetryAsync();

            // Click the Brand filter to expand it if needed, then select the make
            var makeRef = _parser.FindListItemRef(yaml, parameters.Make);
            if (makeRef != null)
            {
                await cli.ClickAsync(makeRef);
                await cli.WaitAsync(2000);
            }

            // Step 3: Select model from model filter
            _logger.LogInformation("[{Provider}] Selecting model: {Model}", Name, parameters.Model);
            yaml = await cli.SnapshotWithRetryAsync();
            var modelRef = _parser.FindListItemRef(yaml, parameters.Model);
            if (modelRef != null)
            {
                await cli.ClickAsync(modelRef);
                await cli.WaitAsync(2000);
            }

            // Step 4: Apply color filter if specified
            if (!string.IsNullOrEmpty(parameters.Color))
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

            // Step 5: Take final snapshot and parse listings
            _logger.LogInformation("[{Provider}] Parsing search results...", Name);
            await cli.WaitAsync(2000);
            yaml = await cli.SnapshotWithRetryAsync();

            result.TotalCount = _parser.ParseResultCount(yaml);
            result.City = _parser.ParseCity(yaml);
            result.Listings = _parser.ParseListings(yaml);
            result.Success = true;

            _logger.LogInformation("[{Provider}] Found {Count} listings (total results: {Total})",
                Name, result.Listings.Count, result.TotalCount);

            await cli.CloseAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{Provider}] Search failed", Name);
            result.Success = false;
            result.ErrorMessage = ex.Message;
            await cli.CloseAsync();
        }

        sw.Stop();
        result.Duration = sw.Elapsed;
        return result;
    }
}
