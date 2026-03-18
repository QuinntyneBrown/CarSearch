using System.Diagnostics;
using CarSearch.Configuration;
using CarSearch.Models;
using CarSearch.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CarSearch.Providers.CoventryNorthLandRover;

public class CoventryNorthLandRoverProvider : ICarSearchProvider
{
    private readonly CoventryNorthLandRoverSnapshotParser _parser;
    private readonly IServiceProvider _serviceProvider;
    private readonly ProviderOptions _options;
    private readonly PlaywrightCliOptions _cliOptions;
    private readonly ILogger<CoventryNorthLandRoverProvider> _logger;

    public string Name => "CoventryNorthLandRover";
    public string DisplayName => "Coventry North Land Rover";
    public bool IsEnabled => _options.Enabled;

    public CoventryNorthLandRoverProvider(
        CoventryNorthLandRoverSnapshotParser parser,
        IServiceProvider serviceProvider,
        IOptionsSnapshot<ProviderOptions> options,
        IOptions<PlaywrightCliOptions> cliOptions,
        ILogger<CoventryNorthLandRoverProvider> logger)
    {
        _parser = parser;
        _serviceProvider = serviceProvider;
        _options = options.Get("CoventryNorthLandRover");
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
            // LeadBox platform uses /new-inventory/ or /pre-owned-inventory/
            var inventoryUrl = _options.BaseUrl.TrimEnd('/') + "/pre-owned-inventory/";
            _logger.LogInformation("[{Provider}] Opening {Url}...", Name, inventoryUrl);
            await cli.OpenAsync(inventoryUrl);
            await cli.WaitAsync(3000);

            // Click the Model filter dropdown
            var yaml = await cli.SnapshotWithRetryAsync();
            var modelFilterRef = _parser.FindFilterRef(yaml, "Model");
            if (modelFilterRef != null)
            {
                await cli.ClickAsync(modelFilterRef);
                await cli.WaitAsync(1000);

                // Look for the model option in the dropdown
                yaml = await cli.SnapshotWithRetryAsync();
                var modelOptionPattern = $@"generic\s+\[ref=([^\]]+)\]\s*\[cursor=pointer\]:\s*{System.Text.RegularExpressions.Regex.Escape(parameters.Model)}";
                var modelMatch = System.Text.RegularExpressions.Regex.Match(yaml, modelOptionPattern);
                if (modelMatch.Success)
                {
                    await cli.ClickAsync(modelMatch.Groups[1].Value);
                    await cli.WaitAsync(2000);
                }
            }

            // Apply color filter if specified
            if (!string.IsNullOrEmpty(parameters.Color))
            {
                yaml = await cli.SnapshotWithRetryAsync();
                var colorFilterRef = _parser.FindFilterRef(yaml, "Colour");
                if (colorFilterRef != null)
                {
                    await cli.ClickAsync(colorFilterRef);
                    await cli.WaitAsync(1000);

                    yaml = await cli.SnapshotWithRetryAsync();
                    var colorPattern = $@"generic\s+\[ref=([^\]]+)\]\s*\[cursor=pointer\]:\s*{System.Text.RegularExpressions.Regex.Escape(parameters.Color)}";
                    var colorMatch = System.Text.RegularExpressions.Regex.Match(yaml, colorPattern);
                    if (colorMatch.Success)
                    {
                        await cli.ClickAsync(colorMatch.Groups[1].Value);
                        await cli.WaitAsync(2000);
                    }
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
