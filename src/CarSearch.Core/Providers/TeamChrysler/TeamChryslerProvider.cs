using System.Diagnostics;
using CarSearch.Configuration;
using CarSearch.Models;
using CarSearch.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CarSearch.Providers.TeamChrysler;

public class TeamChryslerProvider : ICarSearchProvider
{
    private readonly TeamChryslerSnapshotParser _parser;
    private readonly PlaywrightCliService _playwrightCli;
    private readonly ProviderOptions _options;
    private readonly ILogger<TeamChryslerProvider> _logger;

    public string Name => "TeamChrysler";
    public string DisplayName => "Team Chrysler Mississauga";
    public bool IsEnabled => _options.Enabled;

    public TeamChryslerProvider(
        TeamChryslerSnapshotParser parser,
        PlaywrightCliService playwrightCli,
        IOptionsSnapshot<ProviderOptions> options,
        ILogger<TeamChryslerProvider> logger)
    {
        _parser = parser;
        _playwrightCli = playwrightCli;
        _options = options.Get("TeamChrysler");
        _logger = logger;
    }

    public async Task<ProviderSearchResult> SearchAsync(SearchParameters parameters, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var result = new ProviderSearchResult { ProviderName = Name, DisplayName = DisplayName };
        var cli = _playwrightCli.CreateSession(parameters.TimeoutMs, ct);
        try
        {
            // Open inventory page - use make-only filter to avoid & in URL
            // (playwright-cli.cmd on Windows can't handle & in args)
            var make = Uri.EscapeDataString(parameters.Make.ToUpperInvariant());
            var inventoryUrl = _options.BaseUrl.TrimEnd('/') + $"/inventory/used/?make[]={make}";

            _logger.LogInformation("[{Provider}] Opening {Url}...", Name, inventoryUrl);
            await cli.OpenAsync(inventoryUrl);
            await cli.WaitAsync(4000);

            var yaml = await cli.SnapshotWithRetryAsync();

            var baseUrl = _options.BaseUrl.TrimEnd('/');
            var allListings = _parser.ParseListings(yaml, baseUrl);

            // Filter by model in code since URL params with & don't work in cmd.exe
            var modelLower = parameters.Model.ToLowerInvariant();
            var filtered = allListings.Where(l =>
                l.Title != null && l.Title.ToLowerInvariant().Contains(modelLower)).ToList();

            // Apply year filter
            if (parameters.YearFrom.HasValue)
                filtered = filtered.Where(l => l.Year >= parameters.YearFrom.Value).ToList();
            if (parameters.YearTo.HasValue)
                filtered = filtered.Where(l => l.Year <= parameters.YearTo.Value).ToList();

            result.TotalCount = filtered.Count;
            result.City = _parser.ParseCity(yaml);
            result.Listings = filtered;
            result.Success = true;

            _logger.LogInformation("[{Provider}] Found {Count} listings (from {Total} total)", Name, filtered.Count, allListings.Count);
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
        sw.Stop();
        result.Duration = sw.Elapsed;
        return result;
    }
}

