# CarSearch

A .NET 9.0 CLI tool that searches 40+ Canadian dealership websites for vehicle listings using headless browser automation, then produces a consolidated markdown report.

## How It Works

```
CLI args ──> ProviderOrchestrator ──> Provider (per dealership)
                                          │
                                    playwright-cli
                                    open ─ filter ─ snapshot
                                          │
                                    SnapshotParser (regex)
                                          │
                                    VehicleListing[]
                                          │
             Markdown Report <──── MarkdownReportGenerator
```

1. Accepts search criteria via CLI (make, model, postal code, optional filters)
2. Runs each enabled dealership provider sequentially through a shared headless browser
3. Each provider navigates the dealership site, applies filters, and captures an ARIA accessibility snapshot
4. Snapshot parsers extract listing data (title, price, mileage, etc.) using regex
5. Results are aggregated into a markdown report with summary tables and statistics

## Quick Start

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download)
- [playwright-cli](https://www.npmjs.com/package/playwright-cli) installed globally via npm

### Build

```bash
dotnet build
```

### Run

```bash
dotnet run --project src/CarSearch.Cli/CarSearch.Cli.csproj -- \
  --make "Land Rover" \
  --model "Range Rover" \
  --postal-code "L5A4E6" \
  --year-from 2021 \
  --color "White" \
  --timeout 30000
```

### CLI Options

| Option | Required | Description |
|---|---|---|
| `--make` | Yes | Vehicle make (e.g., `"Land Rover"`) |
| `--model` | Yes | Vehicle model (e.g., `"Range Rover"`) |
| `--postal-code` | Yes | Canadian postal code for location |
| `--color` | No | Exterior colour filter |
| `--year-from` | No | Minimum model year |
| `--year-to` | No | Maximum model year |
| `--output` | No | Custom output file path |
| `--timeout` | No | Timeout per browser command in ms (default: `15000`) |

## Configuration

Provider settings and playwright-cli options live in `src/CarSearch.Cli/appsettings.json`.
Machine-local overrides can be placed in `src/CarSearch.Cli/appsettings.Local.json`, which is ignored by git:

```jsonc
{
  "PlaywrightCli": {
    "Command": "playwright-cli",
    "DefaultTimeoutMs": 15000,
    "RetryCount": 3,
    "RetryDelayMs": 2000
  },
  "Providers": {
    "BuddsLandRover": {
      "Enabled": true,
      "BaseUrl": "https://www.buddslandrover.com",
      "Settings": {}
    }
  }
}
```

Each provider can be enabled or disabled individually without code changes.
The current platform-family inventory is documented in `docs/provider-platform-matrix.md`.

## Project Structure

```
CarSearch.sln
├── src/
│   ├── CarSearch.Core/              # Class library
│   │   ├── Configuration/           # PlaywrightCliOptions, ProviderOptions
│   │   ├── Models/                  # SearchParameters, VehicleListing, ProviderSearchResult
│   │   ├── Services/
│   │   │   ├── PlaywrightCliService.cs      # Browser automation wrapper
│   │   │   └── MarkdownReportGenerator.cs   # Report output
│   │   └── Providers/
│   │       ├── ICarSearchProvider.cs        # Provider contract
│   │       ├── ProviderOrchestrator.cs      # Sequential execution coordinator
│   │       ├── Platforms/                   # Shared platform abstractions
│   │       └── <DealerName>/
│   │           ├── <DealerName>Provider.cs          # Site navigation workflow
│   │           └── <DealerName>SnapshotParser.cs    # Regex-based ARIA parser
│   └── CarSearch.Cli/               # Console application
│       ├── Program.cs               # Entry point, DI setup, CLI definition
│       └── appsettings.json         # Runtime configuration
├── tests/
│   └── CarSearch.Core.Tests/        # Parser fixture coverage for shared platform logic
└── docs/
    └── specs/                       # L1/L2 requirements
```

## Adding a Provider

1. Create a folder under `src/CarSearch.Core/Providers/<DealerName>/`
2. Implement `ICarSearchProvider` with the site-specific browser navigation workflow
3. Implement a `SnapshotParser` with regex patterns for the dealership's ARIA snapshot format
4. Add configuration to `appsettings.json` under `Providers`

Providers and parsers are discovered automatically at startup. Adding a provider no longer requires editing the composition root as long as the class follows the `*Provider` naming convention and has a matching configuration section.

Providers on the D2C Media platform now share `Providers/Platforms/D2cMedia/` abstractions. Additional platform families should follow the same pattern instead of copying dealership-specific workflows.

## Supported Dealership Platforms

| Platform | URL Pattern | Filter Style |
|---|---|---|
| D2C Media | `/used/search.html` | Sidebar listitem buttons |
| LeadBox / WordPress | `/pre-owned-inventory/` | Dropdown selects |
| SM360 | `/en/used-inventory` | JS-rendered (limited snapshot support) |

> Some sites (AutoTrader.ca, Cloudflare-protected dealers) block headless browsers and are disabled by default.

## Report Output

The generated markdown report includes:

- **Header** &mdash; search parameters, date, active sources
- **Provider summary table** &mdash; result count, duration, and status per dealership
- **Listings by dealership** &mdash; title, price, mileage, transmission, URL for each vehicle
- **Aggregate statistics** &mdash; price ranges, price bracket distribution, year distribution, new vs. used counts

## License

This project is for personal use.
