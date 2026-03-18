# CarSearch

A .NET 9.0 console application that searches 40+ dealership websites in parallel for vehicle listings using browser automation, then generates an aggregated markdown report.

## How It Works

1. Accepts search criteria via CLI (make, model, postal code, optional filters)
2. Launches headless browser sessions across all enabled dealership providers in parallel
3. Each provider navigates the dealership site, applies filters, and captures an ARIA snapshot
4. Snapshot parsers extract listing data (title, price, mileage, etc.) using regex
5. Results are aggregated into a markdown report with summary tables and statistics

## Usage

```bash
dotnet run --project src/CarSearch/CarSearch.csproj -- \
  --make "Land Rover" \
  --model "Range Rover" \
  --postal-code "L5A4E6" \
  --year-from 2021 \
  --color "White" \
  --timeout 30000
```

### Options

| Option          | Required | Description                              |
|-----------------|----------|------------------------------------------|
| `--make`        | Yes      | Vehicle make (e.g., "Land Rover")        |
| `--model`       | Yes      | Vehicle model (e.g., "Range Rover")      |
| `--postal-code` | Yes      | Canadian postal code for location        |
| `--color`       | No       | Exterior color filter                    |
| `--year-from`   | No       | Minimum model year                       |
| `--year-to`     | No       | Maximum model year                       |
| `--output`      | No       | Custom output file path                  |
| `--timeout`     | No       | Timeout per browser command in ms (default: 15000) |

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download)
- [playwright-cli](https://www.npmjs.com/package/playwright-cli) installed globally via npm

## Configuration

Provider settings and Playwright CLI options are configured in `src/CarSearch/appsettings.json`:

```jsonc
{
  "PlaywrightCli": {
    "Command": "path/to/playwright-cli.cmd",
    "DefaultTimeoutMs": 15000,
    "RetryCount": 3,
    "RetryDelayMs": 2000
  },
  "Providers": {
    "ProviderName": {
      "Enabled": true,
      "BaseUrl": "https://example-dealer.com",
      "Settings": {}
    }
  }
}
```

Each provider can be enabled/disabled individually. Provider-specific settings (like default display count) go in the `Settings` dictionary.

## Architecture

```
src/CarSearch/
  Program.cs                    # CLI entry point & DI setup
  Configuration/                # PlaywrightCliOptions, ProviderOptions
  Models/                       # SearchParameters, VehicleListing, ProviderSearchResult
  Services/
    PlaywrightCliService.cs     # Browser automation via playwright-cli process
    MarkdownReportGenerator.cs  # Aggregated report output
  Providers/
    ICarSearchProvider.cs       # Provider interface
    ProviderOrchestrator.cs     # Parallel execution of all enabled providers
    <DealerName>/
      Provider.cs               # Site-specific browser workflow
      SnapshotParser.cs         # Regex-based ARIA snapshot parser
```

### Adding a Provider

1. Create a new directory under `Providers/`
2. Implement `ICarSearchProvider` (browser navigation workflow)
3. Implement a `SnapshotParser` (regex extraction from ARIA snapshots)
4. Register the parser, provider, and named options in `Program.cs`
5. Add configuration to `appsettings.json` under `Providers`

### Supported Platforms

Providers target several dealership website platforms:

| Platform        | URL Pattern              | Notes                          |
|-----------------|--------------------------|--------------------------------|
| D2C Media       | `/used/search.html`      | Sidebar filter buttons         |
| LeadBox/WordPress | `/pre-owned-inventory/`| Dropdown filters               |
| SM360           | `/en/used-inventory`     | JS-rendered, snapshots may be empty |
| AutoTrader.ca   | autotrader.ca            | Blocked by WAF in headless mode |
| Various         | `/vehicles/`, etc.       | Some blocked by Cloudflare     |

## Output

The generated markdown report includes:

- Search parameters and date
- Provider summary table (results, duration, status per source)
- Vehicle listings grouped by dealership with price, mileage, transmission, and links
- Aggregate statistics: price ranges, year distribution, new vs. used counts
