using System.Diagnostics;
using CarSearch.Configuration;
using CarSearch.Models;
using CarSearch.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CarSearch.Providers.BuddsLandRover;

public class BuddsLandRoverProvider : ICarSearchProvider
{
    private readonly BuddsLandRoverSnapshotParser _parser;
    private readonly IServiceProvider _serviceProvider;
    private readonly ProviderOptions _options;
    private readonly PlaywrightCliOptions _cliOptions;
    private readonly ILogger<BuddsLandRoverProvider> _logger;

    public string Name => "BuddsLandRover";
    public string DisplayName => "Budds' Land Rover Oakville";
    public bool IsEnabled => _options.Enabled;

    public BuddsLandRoverProvider(
        BuddsLandRoverSnapshotParser parser,
        IServiceProvider serviceProvider,
        IOptionsSnapshot<ProviderOptions> options,
        IOptions<PlaywrightCliOptions> cliOptions,
        ILogger<BuddsLandRoverProvider> logger)
    {
        _parser = parser;
        _serviceProvider = serviceProvider;
        _options = options.Get("BuddsLandRover");
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
            var inventoryUrl = _options.BaseUrl.TrimEnd('/') + "/used/search.html";
            _logger.LogInformation("[{Provider}] Opening {Url}...", Name, inventoryUrl);
            await cli.OpenAsync(inventoryUrl);
            await cli.WaitAsync(3000);

            var yaml = await cli.SnapshotWithRetryAsync();

            var makeRef = _parser.FindListItemRef(yaml, parameters.Make);
            if (makeRef != null)
            {
                await cli.ClickAsync(makeRef);
                await cli.WaitAsync(2000);
            }

            yaml = await cli.SnapshotWithRetryAsync();
            var modelRef = _parser.FindListItemRef(yaml, parameters.Model);
            if (modelRef != null)
            {
                await cli.ClickAsync(modelRef);
                await cli.WaitAsync(2000);
            }

            if (!string.IsNullOrEmpty(parameters.Color))
            {
                yaml = await cli.SnapshotWithRetryAsync();
                var colorRef = _parser.FindColorRef(yaml, parameters.Color);
                if (colorRef != null)
                {
                    await cli.ClickAsync(colorRef);
                    await cli.WaitAsync(2000);
                }
            }

            await cli.WaitAsync(2000);
            yaml = await cli.SnapshotWithRetryAsync();

            result.TotalCount = _parser.ParseResultCount(yaml);
            result.City = _parser.ParseCity(yaml);
            result.Listings = _parser.ParseListings(yaml);
            result.Success = true;

            _logger.LogInformation("[{Provider}] Found {Count} listings", Name, result.Listings.Count);
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
