using CarSearch.Configuration;
using CarSearch.Providers.Platforms.D2cMedia;
using CarSearch.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CarSearch.Providers.LandRoverMetroWest;

public class LandRoverMetroWestProvider : D2cMediaProviderBase<LandRoverMetroWestSnapshotParser>
{
    private static readonly D2cMediaProviderDefinition Definition =
        new("LandRoverMetroWest", "Land Rover Metro West", "/used/search.html");

    public LandRoverMetroWestProvider(
        LandRoverMetroWestSnapshotParser parser,
        PlaywrightCliService playwrightCli,
        IOptionsSnapshot<ProviderOptions> options,
        ILogger<LandRoverMetroWestProvider> logger)
        : base(parser, playwrightCli, options, logger, Definition)
    {
    }
}
