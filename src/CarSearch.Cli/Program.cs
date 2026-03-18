using System.CommandLine;
using System.CommandLine.Parsing;
using CarSearch.Configuration;
using CarSearch.Models;
using CarSearch.Providers;
using CarSearch.Providers.AutoTrader;
using CarSearch.Providers.Clutch;
using CarSearch.Providers.KijijiAutos;
using CarSearch.Providers.LandRoverBrampton;
using CarSearch.Providers.LandRoverMetroWest;
using CarSearch.Providers.BuddsLandRover;
using CarSearch.Providers.CoventryNorthLandRover;
using CarSearch.Providers.LandRoverToronto;
using CarSearch.Providers.MercedesMississauga;
using CarSearch.Providers.JaguarThornhill;
using CarSearch.Providers.BmwToronto;
using CarSearch.Providers.BmwEtobicoke;
using CarSearch.Providers.MaranelloBmw;
using CarSearch.Providers.ParkviewBmw;
using CarSearch.Providers.MississaugaHonda;
using CarSearch.Providers.TeamChrysler;
using CarSearch.Providers.OntarioChrysler;
using CarSearch.Providers.GatewayChevrolet;
using CarSearch.Providers.RichmondHillChrysler;
using CarSearch.Providers.JaguarToronto;
using CarSearch.Providers.NorthwestLexus;
using CarSearch.Providers.BramptonEastToyota;
using CarSearch.Providers.ErinMillsMazda;
using CarSearch.Providers.SubaruMississauga;
using CarSearch.Providers.AcuraNorthMississauga;
using CarSearch.Providers.ErinMillsAcura;
using CarSearch.Providers.PolicaroAcura;
using CarSearch.Providers.PerformanceInfiniti;
using CarSearch.Providers.KiaOfBrampton;
using CarSearch.Providers.AirportKia;
using CarSearch.Providers.MississaugaKia;
using CarSearch.Providers.FourZeroOneDixieHyundai;
using CarSearch.Providers.MississaugaHyundai;
using CarSearch.Providers.PerformanceHyundai;
using CarSearch.Providers.PlanetFord;
using CarSearch.Providers.FormulaFordLincoln;
using CarSearch.Providers.MohawkFord;
using CarSearch.Providers.OakvilleNissan;
using CarSearch.Providers.MiltonNissan;
using CarSearch.Providers.BramptonNorthNissan;
using CarSearch.Providers.BramptonChrysler;
using CarSearch.Providers.PeelChrysler;
using CarSearch.Providers.BmwAutohaus;
using CarSearch.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .Build();

var services = new ServiceCollection();

services.AddSingleton<IConfiguration>(configuration);

services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});

services.Configure<PlaywrightCliOptions>(configuration.GetSection("PlaywrightCli"));

// Bind per-provider options using named options
var providerNames = new[] {
    "AutoTrader", "Clutch", "KijijiAutos",
    "LandRoverBrampton", "LandRoverMetroWest", "BuddsLandRover", "CoventryNorthLandRover", "LandRoverToronto",
    "MercedesMississauga", "JaguarThornhill", "BmwToronto", "BmwEtobicoke", "MaranelloBmw", "ParkviewBmw",
    "MississaugaHonda", "TeamChrysler", "OntarioChrysler", "GatewayChevrolet",
    "RichmondHillChrysler", "JaguarToronto", "NorthwestLexus", "BramptonEastToyota",
    "ErinMillsMazda", "SubaruMississauga", "AcuraNorthMississauga", "ErinMillsAcura",
    "PolicaroAcura", "PerformanceInfiniti", "KiaOfBrampton", "AirportKia", "MississaugaKia",
    "FourZeroOneDixieHyundai", "MississaugaHyundai", "PerformanceHyundai",
    "PlanetFord", "FormulaFordLincoln", "MohawkFord",
    "OakvilleNissan", "MiltonNissan", "BramptonNorthNissan",
    "BramptonChrysler", "PeelChrysler", "BmwAutohaus"
};
foreach (var name in providerNames)
    services.Configure<ProviderOptions>(name, configuration.GetSection($"Providers:{name}"));

// Register provider-specific services
services.AddTransient<AutoTraderSnapshotParser>();
services.AddTransient<ClutchSnapshotParser>();
services.AddTransient<KijijiAutosSnapshotParser>();
services.AddTransient<LandRoverBramptonSnapshotParser>();
services.AddTransient<LandRoverMetroWestSnapshotParser>();
services.AddTransient<BuddsLandRoverSnapshotParser>();
services.AddTransient<CoventryNorthLandRoverSnapshotParser>();
services.AddTransient<LandRoverTorontoSnapshotParser>();
services.AddTransient<MercedesMississaugaSnapshotParser>();
services.AddTransient<JaguarThornhillSnapshotParser>();
services.AddTransient<BmwTorontoSnapshotParser>();
services.AddTransient<BmwEtobicokeSnapshotParser>();
services.AddTransient<MaranelloBmwSnapshotParser>();
services.AddTransient<ParkviewBmwSnapshotParser>();
services.AddTransient<MississaugaHondaSnapshotParser>();
services.AddTransient<TeamChryslerSnapshotParser>();
services.AddTransient<OntarioChryslerSnapshotParser>();
services.AddTransient<GatewayChevroletSnapshotParser>();
services.AddTransient<RichmondHillChryslerSnapshotParser>();
services.AddTransient<JaguarTorontoSnapshotParser>();
services.AddTransient<NorthwestLexusSnapshotParser>();
services.AddTransient<BramptonEastToyotaSnapshotParser>();
services.AddTransient<ErinMillsMazdaSnapshotParser>();
services.AddTransient<SubaruMississaugaSnapshotParser>();
services.AddTransient<AcuraNorthMississaugaSnapshotParser>();
services.AddTransient<ErinMillsAcuraSnapshotParser>();
services.AddTransient<PolicaroAcuraSnapshotParser>();
services.AddTransient<PerformanceInfinitiSnapshotParser>();
services.AddTransient<KiaOfBramptonSnapshotParser>();
services.AddTransient<AirportKiaSnapshotParser>();
services.AddTransient<MississaugaKiaSnapshotParser>();
services.AddTransient<FourZeroOneDixieHyundaiSnapshotParser>();
services.AddTransient<MississaugaHyundaiSnapshotParser>();
services.AddTransient<PerformanceHyundaiSnapshotParser>();
services.AddTransient<PlanetFordSnapshotParser>();
services.AddTransient<FormulaFordLincolnSnapshotParser>();
services.AddTransient<MohawkFordSnapshotParser>();
services.AddTransient<OakvilleNissanSnapshotParser>();
services.AddTransient<MiltonNissanSnapshotParser>();
services.AddTransient<BramptonNorthNissanSnapshotParser>();
services.AddTransient<BramptonChryslerSnapshotParser>();
services.AddTransient<PeelChryslerSnapshotParser>();
services.AddTransient<BmwAutohausSnapshotParser>();

// Register providers
services.AddTransient<ICarSearchProvider, AutoTraderProvider>();
services.AddTransient<ICarSearchProvider, ClutchProvider>();
services.AddTransient<ICarSearchProvider, KijijiAutosProvider>();
services.AddTransient<ICarSearchProvider, LandRoverBramptonProvider>();
services.AddTransient<ICarSearchProvider, LandRoverMetroWestProvider>();
services.AddTransient<ICarSearchProvider, BuddsLandRoverProvider>();
services.AddTransient<ICarSearchProvider, CoventryNorthLandRoverProvider>();
services.AddTransient<ICarSearchProvider, LandRoverTorontoProvider>();
services.AddTransient<ICarSearchProvider, MercedesMississaugaProvider>();
services.AddTransient<ICarSearchProvider, JaguarThornhillProvider>();
services.AddTransient<ICarSearchProvider, BmwTorontoProvider>();
services.AddTransient<ICarSearchProvider, BmwEtobicokeProvider>();
services.AddTransient<ICarSearchProvider, MaranelloBmwProvider>();
services.AddTransient<ICarSearchProvider, ParkviewBmwProvider>();
services.AddTransient<ICarSearchProvider, MississaugaHondaProvider>();
services.AddTransient<ICarSearchProvider, TeamChryslerProvider>();
services.AddTransient<ICarSearchProvider, OntarioChryslerProvider>();
services.AddTransient<ICarSearchProvider, GatewayChevroletProvider>();
services.AddTransient<ICarSearchProvider, RichmondHillChryslerProvider>();
services.AddTransient<ICarSearchProvider, JaguarTorontoProvider>();
services.AddTransient<ICarSearchProvider, NorthwestLexusProvider>();
services.AddTransient<ICarSearchProvider, BramptonEastToyotaProvider>();
services.AddTransient<ICarSearchProvider, ErinMillsMazdaProvider>();
services.AddTransient<ICarSearchProvider, SubaruMississaugaProvider>();
services.AddTransient<ICarSearchProvider, AcuraNorthMississaugaProvider>();
services.AddTransient<ICarSearchProvider, ErinMillsAcuraProvider>();
services.AddTransient<ICarSearchProvider, PolicaroAcuraProvider>();
services.AddTransient<ICarSearchProvider, PerformanceInfinitiProvider>();
services.AddTransient<ICarSearchProvider, KiaOfBramptonProvider>();
services.AddTransient<ICarSearchProvider, AirportKiaProvider>();
services.AddTransient<ICarSearchProvider, MississaugaKiaProvider>();
services.AddTransient<ICarSearchProvider, FourZeroOneDixieHyundaiProvider>();
services.AddTransient<ICarSearchProvider, MississaugaHyundaiProvider>();
services.AddTransient<ICarSearchProvider, PerformanceHyundaiProvider>();
services.AddTransient<ICarSearchProvider, PlanetFordProvider>();
services.AddTransient<ICarSearchProvider, FormulaFordLincolnProvider>();
services.AddTransient<ICarSearchProvider, MohawkFordProvider>();
services.AddTransient<ICarSearchProvider, OakvilleNissanProvider>();
services.AddTransient<ICarSearchProvider, MiltonNissanProvider>();
services.AddTransient<ICarSearchProvider, BramptonNorthNissanProvider>();
services.AddTransient<ICarSearchProvider, BramptonChryslerProvider>();
services.AddTransient<ICarSearchProvider, PeelChryslerProvider>();
services.AddTransient<ICarSearchProvider, BmwAutohausProvider>();

// Register orchestrator and report generator
services.AddTransient<ProviderOrchestrator>();
services.AddSingleton<MarkdownReportGenerator>();

var serviceProvider = services.BuildServiceProvider();

// Define CLI options
var makeOption = new Option<string>("--make") { Description = "Vehicle make", Required = true };
var modelOption = new Option<string>("--model") { Description = "Vehicle model", Required = true };
var postalCodeOption = new Option<string>("--postal-code") { Description = "Canadian postal code", Required = true };
var colorOption = new Option<string?>("--color") { Description = "Exterior color filter" };
var yearFromOption = new Option<int?>("--year-from") { Description = "Minimum model year" };
var yearToOption = new Option<int?>("--year-to") { Description = "Maximum model year" };
var outputOption = new Option<string?>("--output") { Description = "Output markdown file path" };
var timeoutOption = new Option<int>("--timeout") { Description = "Timeout per playwright-cli command (ms)", DefaultValueFactory = _ => 15000 };

var rootCommand = new RootCommand("Multi-source vehicle search tool")
{
    makeOption,
    modelOption,
    postalCodeOption,
    colorOption,
    yearFromOption,
    yearToOption,
    outputOption,
    timeoutOption
};

rootCommand.SetAction(async (ParseResult parseResult, CancellationToken cancellationToken) =>
{
    var make = parseResult.GetValue(makeOption)!;
    var model = parseResult.GetValue(modelOption)!;
    var postalCode = parseResult.GetValue(postalCodeOption)!;
    var color = parseResult.GetValue(colorOption);
    var yearFrom = parseResult.GetValue(yearFromOption);
    var yearTo = parseResult.GetValue(yearToOption);
    var output = parseResult.GetValue(outputOption);
    var timeout = parseResult.GetValue(timeoutOption);

    var parameters = new SearchParameters
    {
        Make = make,
        Model = model,
        PostalCode = postalCode,
        Color = color,
        YearFrom = yearFrom,
        YearTo = yearTo,
        OutputPath = output,
        TimeoutMs = timeout
    };

    // Update timeout in options if specified
    var cliOptions = serviceProvider.GetRequiredService<IOptions<PlaywrightCliOptions>>();
    cliOptions.Value.DefaultTimeoutMs = timeout;

    var orchestrator = serviceProvider.GetRequiredService<ProviderOrchestrator>();
    var reportGenerator = serviceProvider.GetRequiredService<MarkdownReportGenerator>();

    try
    {
        var results = await orchestrator.SearchAllAsync(parameters, cancellationToken);

        // Generate report
        var report = reportGenerator.Generate(results, parameters);

        var outputPath = parameters.OutputPath ?? GenerateOutputPath(parameters, results);
        await File.WriteAllTextAsync(outputPath, report, cancellationToken);

        Console.WriteLine($"Search complete! Report saved to: {outputPath}");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Search failed: {ex.Message}");
        return 1;
    }

    return 0;
});

var result = rootCommand.Parse(args);
return await result.InvokeAsync();

static string GenerateOutputPath(SearchParameters parameters, List<ProviderSearchResult> results)
{
    var city = results.FirstOrDefault(r => r.Success && r.City != null)?.City;

    var parts = new List<string>();
    if (parameters.Color != null)
        parts.Add(parameters.Color.ToLowerInvariant());
    parts.Add(parameters.Make.ToLowerInvariant().Replace(" ", "-"));
    parts.Add(parameters.Model.ToLowerInvariant().Replace(" ", "-"));
    if (city != null)
        parts.Add(city.ToLowerInvariant().Replace(" ", "-"));

    var filename = string.Join("-", parts) + ".md";
    return Path.Combine(Directory.GetCurrentDirectory(), filename);
}
