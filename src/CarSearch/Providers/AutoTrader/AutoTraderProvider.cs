using System.Diagnostics;
using CarSearch.Configuration;
using CarSearch.Models;
using CarSearch.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CarSearch.Providers.AutoTrader;

public class AutoTraderProvider : ICarSearchProvider
{
    private readonly AutoTraderSnapshotParser _parser;
    private readonly IServiceProvider _serviceProvider;
    private readonly ProviderOptions _options;
    private readonly PlaywrightCliOptions _cliOptions;
    private readonly ILogger<AutoTraderProvider> _logger;

    public string Name => "AutoTrader";
    public string DisplayName => "AutoTrader.ca";
    public bool IsEnabled => _options.Enabled;

    public AutoTraderProvider(
        AutoTraderSnapshotParser parser,
        IServiceProvider serviceProvider,
        IOptionsSnapshot<ProviderOptions> options,
        IOptions<PlaywrightCliOptions> cliOptions,
        ILogger<AutoTraderProvider> logger)
    {
        _parser = parser;
        _serviceProvider = serviceProvider;
        _options = options.Get("AutoTrader");
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

        // Create a dedicated PlaywrightCliService for this provider
        var cli = new PlaywrightCliService(
            Options.Create(_cliOptions),
            Microsoft.Extensions.Logging.LoggerFactoryExtensions.CreateLogger<PlaywrightCliService>(
                (ILoggerFactory)_serviceProvider.GetService(typeof(ILoggerFactory))!));

        try
        {
            // Step 1: Open AutoTrader.ca
            _logger.LogInformation("[{Provider}] Opening {Url}...", Name, _options.BaseUrl);
            await cli.OpenAsync(_options.BaseUrl);
            await cli.WaitAsync(2000);

            // Step 2: Accept cookie consent (if present)
            await TryDismissCookieConsent(cli);

            // Step 3: Select make from dropdown
            _logger.LogInformation("[{Provider}] Selecting make: {Make}", Name, parameters.Make);
            var yaml = await cli.SnapshotWithRetryAsync();
            var makeRef = _parser.FindComboboxRef(yaml, "cars-make-filter");
            if (makeRef == null)
                throw new InvalidOperationException("Could not find make dropdown");

            await cli.SelectAsync(makeRef, parameters.Make);
            await cli.WaitAsync(1500);

            // Step 4: Fill model in autocomplete, select suggestion
            _logger.LogInformation("[{Provider}] Entering model: {Model}", Name, parameters.Model);
            yaml = await cli.SnapshotWithRetryAsync();

            var modelRef = _parser.FindComboboxRefContaining(yaml, "models");
            if (modelRef == null)
                throw new InvalidOperationException("Could not find model input");

            await cli.FillAsync(modelRef, parameters.Model);
            await cli.WaitAsync(1000);

            yaml = await cli.SnapshotWithRetryAsync();
            var suggestionRef = _parser.FindListboxOptionRef(yaml, parameters.Model);
            if (suggestionRef != null)
            {
                await cli.ClickAsync(suggestionRef);
                await cli.WaitAsync(1000);
            }

            // Step 5: Fill postal code
            _logger.LogInformation("[{Provider}] Entering postal code: {PostalCode}", Name, parameters.PostalCode);
            yaml = await cli.SnapshotWithRetryAsync();
            var postalRef = _parser.FindComboboxRefContaining(yaml, "Postal code");
            if (postalRef == null)
                throw new InvalidOperationException("Could not find postal code input");

            await cli.FillAsync(postalRef, parameters.PostalCode);
            await cli.WaitAsync(2000);

            yaml = await cli.SnapshotWithRetryAsync();
            var postalSuggestion = _parser.FindFirstListboxOption(yaml);
            if (postalSuggestion != null)
            {
                await cli.ClickAsync(postalSuggestion);
                await cli.WaitAsync(1000);
            }

            // Step 6: Click results link to navigate to search results
            _logger.LogInformation("[{Provider}] Navigating to search results...", Name);
            yaml = await cli.SnapshotWithRetryAsync();
            var resultsRef = _parser.FindResultsLinkRef(yaml);
            if (resultsRef == null)
                throw new InvalidOperationException("Could not find results link");

            await cli.ClickAsync(resultsRef);
            await cli.WaitAsync(3000);

            // Step 7: Dismiss welcome popup
            await TryDismissWelcomePopup(cli);

            // Step 8: Apply model year filter (if specified)
            if (parameters.YearFrom.HasValue || parameters.YearTo.HasValue)
            {
                await ApplyYearFilter(cli, parameters.YearFrom, parameters.YearTo);
            }

            // Step 9: Apply exterior color filter (if specified)
            if (!string.IsNullOrEmpty(parameters.Color))
            {
                await ApplyColorFilter(cli, parameters.Color);
            }

            // Step 10: Change display count
            var displayCount = _options.Settings.TryGetValue("DefaultDisplayCount", out var dc)
                ? int.Parse(dc) : 100;
            await ChangeDisplayCount(cli, displayCount);

            // Step 11: Take final snapshot and parse listings
            _logger.LogInformation("[{Provider}] Parsing search results...", Name);
            await cli.WaitAsync(2000);
            yaml = await cli.SnapshotWithRetryAsync();

            result.TotalCount = _parser.ParseResultCount(yaml);
            result.City = _parser.ParseCity(yaml);
            result.Listings = _parser.ParseListings(yaml);
            result.Success = true;

            _logger.LogInformation("[{Provider}] Found {Count} listings (total results: {Total})",
                Name, result.Listings.Count, result.TotalCount);

            // Step 12: Close browser
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

    private async Task TryDismissCookieConsent(PlaywrightCliService cli)
    {
        try
        {
            var yaml = await cli.SnapshotWithRetryAsync();
            var acceptRef = _parser.FindButtonRef(yaml, "Accept");
            if (acceptRef != null)
            {
                _logger.LogDebug("[{Provider}] Dismissing cookie consent...", Name);
                await cli.ClickAsync(acceptRef);
                await cli.WaitAsync(500);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug("[{Provider}] No cookie consent found or failed to dismiss: {Error}", Name, ex.Message);
        }
    }

    private async Task TryDismissWelcomePopup(PlaywrightCliService cli)
    {
        try
        {
            _logger.LogDebug("[{Provider}] Attempting to dismiss welcome popup...", Name);
            await cli.RunCodeAsync("async page => { const el = await page.$('#welcome-popup'); if (el) await el.evaluate(e => e.style.display = 'none'); }");
            await cli.WaitAsync(500);
        }
        catch (Exception ex)
        {
            _logger.LogDebug("[{Provider}] No welcome popup or failed to dismiss: {Error}", Name, ex.Message);
        }
    }

    private async Task ApplyYearFilter(PlaywrightCliService cli, int? yearFrom, int? yearTo)
    {
        _logger.LogInformation("[{Provider}] Applying year filter: {YearFrom} - {YearTo}", Name, yearFrom, yearTo);

        var yaml = await cli.SnapshotWithRetryAsync();
        var yearFilterRef = _parser.FindButtonRef(yaml, "Model year filter");
        if (yearFilterRef == null)
        {
            _logger.LogWarning("[{Provider}] Could not find year filter button", Name);
            return;
        }

        await cli.ClickAsync(yearFilterRef);
        await cli.WaitAsync(1000);

        yaml = await cli.SnapshotWithRetryAsync();

        if (yearFrom.HasValue)
        {
            var minRef = _parser.FindComboboxRefContaining(yaml, "Minimum model year");
            if (minRef != null)
            {
                await cli.ClickAsync(minRef);
                await cli.WaitAsync(500);

                yaml = await cli.SnapshotWithRetryAsync();
                var yearOptionRef = _parser.FindOptionRef(yaml, yearFrom.Value.ToString());
                if (yearOptionRef != null)
                {
                    await cli.ClickAsync(yearOptionRef);
                    await cli.WaitAsync(1000);
                }
            }
        }

        if (yearTo.HasValue)
        {
            yaml = await cli.SnapshotWithRetryAsync();
            var maxRef = _parser.FindComboboxRefContaining(yaml, "Maximum model year");
            if (maxRef != null)
            {
                await cli.ClickAsync(maxRef);
                await cli.WaitAsync(500);

                yaml = await cli.SnapshotWithRetryAsync();
                var yearOptionRef = _parser.FindOptionRef(yaml, yearTo.Value.ToString());
                if (yearOptionRef != null)
                {
                    await cli.ClickAsync(yearOptionRef);
                    await cli.WaitAsync(1000);
                }
            }
        }

        yaml = await cli.SnapshotWithRetryAsync();
        var applyRef = _parser.FindApplyButtonRef(yaml);
        if (applyRef != null)
        {
            await cli.ClickAsync(applyRef);
            await cli.WaitAsync(2000);
        }
    }

    private async Task ApplyColorFilter(PlaywrightCliService cli, string color)
    {
        _logger.LogInformation("[{Provider}] Applying color filter: {Color}", Name, color);

        var yaml = await cli.SnapshotWithRetryAsync();
        var colorFilterRef = _parser.FindButtonRef(yaml, "Exterior color filter");
        if (colorFilterRef == null)
        {
            _logger.LogWarning("[{Provider}] Could not find color filter button", Name);
            return;
        }

        await cli.ClickAsync(colorFilterRef);
        await cli.WaitAsync(1000);

        yaml = await cli.SnapshotWithRetryAsync();
        var checkboxRef = _parser.FindCheckboxRef(yaml, color);
        if (checkboxRef == null)
        {
            _logger.LogWarning("[{Provider}] Could not find checkbox for color: {Color}", Name, color);
            return;
        }

        await cli.ClickAsync(checkboxRef);
        await cli.WaitAsync(1000);

        yaml = await cli.SnapshotWithRetryAsync();
        var applyRef = _parser.FindApplyButtonRef(yaml);
        if (applyRef != null)
        {
            await cli.ClickAsync(applyRef);
            await cli.WaitAsync(2000);
        }
    }

    private async Task ChangeDisplayCount(PlaywrightCliService cli, int count)
    {
        _logger.LogInformation("[{Provider}] Changing display count to {Count}...", Name, count);

        var yaml = await cli.SnapshotWithRetryAsync();
        var displayRef = _parser.FindDisplayComboboxRef(yaml);
        if (displayRef == null)
        {
            _logger.LogWarning("[{Provider}] Could not find display count dropdown", Name);
            return;
        }

        await cli.SelectAsync(displayRef, count.ToString());
        await cli.WaitAsync(3000);
    }
}
