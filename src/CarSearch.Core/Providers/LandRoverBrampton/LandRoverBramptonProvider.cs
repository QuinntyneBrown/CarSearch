using CarSearch.Configuration;
using CarSearch.Providers.Platforms.D2cMedia;
using CarSearch.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CarSearch.Providers.LandRoverBrampton;

public class LandRoverBramptonProvider : D2cMediaProviderBase<LandRoverBramptonSnapshotParser>
{
    private static readonly D2cMediaProviderDefinition Definition =
        new("LandRoverBrampton", "Land Rover Brampton", "/used/search.html");

    public LandRoverBramptonProvider(
        LandRoverBramptonSnapshotParser parser,
        PlaywrightCliService playwrightCli,
        IOptionsSnapshot<ProviderOptions> options,
        ILogger<LandRoverBramptonProvider> logger)
        : base(parser, playwrightCli, options, logger, Definition)
    {
    }
}
