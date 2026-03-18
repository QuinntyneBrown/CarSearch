using CarSearch.Configuration;
using CarSearch.Providers.Platforms.D2cMedia;
using CarSearch.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CarSearch.Providers.ParkviewBmw;

public class ParkviewBmwProvider : D2cMediaProviderBase<ParkviewBmwSnapshotParser>
{
    private static readonly D2cMediaProviderDefinition Definition =
        new("ParkviewBmw", "Parkview BMW Toronto", "/used/search.html", "Parkview BMW");

    public ParkviewBmwProvider(
        ParkviewBmwSnapshotParser parser,
        PlaywrightCliService playwrightCli,
        IOptionsSnapshot<ProviderOptions> options,
        ILogger<ParkviewBmwProvider> logger)
        : base(parser, playwrightCli, options, logger, Definition)
    {
    }
}
