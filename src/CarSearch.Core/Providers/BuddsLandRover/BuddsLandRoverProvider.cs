using CarSearch.Configuration;
using CarSearch.Providers.Platforms.D2cMedia;
using CarSearch.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CarSearch.Providers.BuddsLandRover;

public class BuddsLandRoverProvider : D2cMediaProviderBase<BuddsLandRoverSnapshotParser>
{
    private static readonly D2cMediaProviderDefinition Definition =
        new("BuddsLandRover", "Budds' Land Rover Oakville", "/used/search.html");

    public BuddsLandRoverProvider(
        BuddsLandRoverSnapshotParser parser,
        PlaywrightCliService playwrightCli,
        IOptionsSnapshot<ProviderOptions> options,
        ILogger<BuddsLandRoverProvider> logger)
        : base(parser, playwrightCli, options, logger, Definition)
    {
    }
}
