using System.CommandLine;
using System.CommandLine.Parsing;
using CarSearch.Configuration;
using CarSearch.Models;
using CarSearch.Providers;
using CarSearch.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile("appsettings.Local.json", optional: true)
    .Build();

var services = new ServiceCollection();

services.AddSingleton<IConfiguration>(configuration);

services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});

services.Configure<PlaywrightCliOptions>(configuration.GetSection("PlaywrightCli"));
services.AddSingleton<PlaywrightCliService>();

RegisterProviderOptions(services, configuration);
RegisterProviderServices(services, configuration);

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
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
    {
        Console.Error.WriteLine("Search canceled.");
        return 1;
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

static void RegisterProviderOptions(IServiceCollection services, IConfiguration configuration)
{
    foreach (var providerSection in configuration.GetSection("Providers").GetChildren())
    {
        services.Configure<ProviderOptions>(providerSection.Key, providerSection);
    }
}

static void RegisterProviderServices(IServiceCollection services, IConfiguration configuration)
{
    var coreAssembly = typeof(ICarSearchProvider).Assembly;

    foreach (var parserType in coreAssembly.DefinedTypes
                 .Where(type => type is { IsClass: true, IsAbstract: false } &&
                                type.Name.EndsWith("SnapshotParser", StringComparison.Ordinal)))
    {
        services.AddTransient(parserType.AsType());
    }

    var providerTypesByName = coreAssembly.DefinedTypes
        .Where(type => type is { IsClass: true, IsAbstract: false } &&
                       typeof(ICarSearchProvider).IsAssignableFrom(type.AsType()))
        .ToDictionary(
            type => GetProviderName(type.AsType()),
            type => type.AsType(),
            StringComparer.OrdinalIgnoreCase);

    foreach (var providerSection in configuration.GetSection("Providers").GetChildren())
    {
        if (providerTypesByName.TryGetValue(providerSection.Key, out var providerType))
        {
            services.AddTransient(typeof(ICarSearchProvider), providerType);
        }
    }
}

static string GetProviderName(Type providerType)
{
    const string suffix = "Provider";
    return providerType.Name.EndsWith(suffix, StringComparison.Ordinal)
        ? providerType.Name[..^suffix.Length]
        : providerType.Name;
}
